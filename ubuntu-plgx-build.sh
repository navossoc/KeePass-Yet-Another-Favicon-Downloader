#!/bin/bash

PROJDIR=$(dirname ${0})         # directory name of the build script

# Configuration
SOURCE=${PROJDIR}/YAFD
TARGET=${PROJDIR}/publish             # Copy the source here pefore building
PLGX_LOC="/usr/lib/keepass2/Plugins"  # Where to install the plugin
KEEPASS=$(which keepass2)             # keepass binary location
NAME=YetAnotherFaviconDownloader


# Clean old files
echo Cleaning...
if [ -d "${TARGET}" ]; then           # remove the build dir
    rm -r ${TARGET}
fi
if [ -e publish.plgx ]; then          # remove the compiled binary
    rm publish.plgx
fi
if [ -e ${NAME}.plgx ]; then          # remove renamed binary
    rm ${NAME}.plgx
fi

# Copy the files needed to build the plugin
echo Copying...
cp -r ${SOURCE} ${TARGET}

# Let KeePass do its magic
echo Building...
${KEEPASS} --plgx-create "${TARGET}" --plgx-prereq-kp:2.34
mv publish.plgx ${NAME}.plgx

# Deploy PLGX file
echo Deploying...
sudo cp ${NAME}.plgx ${PLGX_LOC}
