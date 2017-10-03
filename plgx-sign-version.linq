<Query Kind="Statements">
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

//
// This code snippet generate a RSA public/private key pair and
// sign a version information file using SHA-512 and UTF-8 encoding.
//

// Configuration
const int keySize = 4096;
const string keyFolder = "bin";

string privateKeyFile, publicKeyFile, versionFile;
try
{
	// Remember to save this file, otherwise this will not work!
	var path = Path.GetDirectoryName(Util.CurrentQueryPath);
	Directory.SetCurrentDirectory(path);

	Directory.CreateDirectory(Path.Combine(path, keyFolder));
	privateKeyFile = Path.Combine(path, keyFolder, "private.xml");
	publicKeyFile = Path.Combine(path, keyFolder, "public.xml");
	versionFile = Path.Combine(path, "VERSION");
}
catch (Exception ex)
{
	throw new Exception("This file must be in the root folder of the project.", ex);
}

// Creates a new key pair only if it doesn't already exists
if (!File.Exists(privateKeyFile))
{
	using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize))
	{
		rsa.PersistKeyInCsp = false;
		File.WriteAllText(privateKeyFile, rsa.ToXmlString(true));
		File.WriteAllText(publicKeyFile, rsa.ToXmlString(false));
	}
}

// Don't lose or share this file!
var privateKey = File.ReadAllText(privateKeyFile);

// To sign an unsigned version information file, hash all trimmed non-empty 
// lines between the header and the footer line using SHA-512, UTF-8 
// encoding, each line terminated by '\n' (not "\r\n"). Sign the hash using 
// the private key (if you're using RSACryptoServiceProvider: load the 
// private key using its FromXmlString method, then compute the signature 
// using the SignData method). Encode the hash using Base64 and append it 
// to the first line of the version information file. 

// Reference: http://keepass.info/help/v2_dev/plg_index.html

const string separator = ":", newLine = "\n";

var sb = new StringBuilder();

// Removes all invalid lines
var lines = File.ReadAllLines(versionFile);
for (var i = 0; i < lines.Length; i++)
{
	var line = lines[i].Trim();
	if (line == string.Empty || line.StartsWith(separator))
	{
		continue;
	}

	sb.Append(line);
	sb.Append(newLine);
}

using (var sha512 = SHA512.Create())
{
	var data = Encoding.UTF8.GetBytes(sb.ToString());
	using (var rsa = RSACryptoServiceProvider.Create())
	{
		// Load private key
		rsa.FromXmlString(privateKey);

		// Sign the hash
		var signedHash = rsa.SignData(data, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
		var base64SignedHash = Convert.ToBase64String(signedHash);

		// Header
		sb.Insert(0, string.Format("{0}{1}{2}", separator, base64SignedHash, newLine));

		// Footer
		sb.Append(separator);

		File.WriteAllText(versionFile, sb.ToString().Dump());
	}
}

Console.WriteLine();
Console.WriteLine("Don't forget to change version on file YAFD/Properties/AssemblyInfo.cs");

