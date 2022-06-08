using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System.Collections.ObjectModel;
using System.Security;
using System.IO;
using System.Text;
using System;
using System.Runtime.InteropServices;
using CSharp_easy_RSA_PEM;      //DLL
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

public class RSAMode 
{


	static string _x = "";

	static UTF8Encoding ByteConv = new UTF8Encoding();

	static bool verbose = false;
	//Unity pem files path
	static string path_priv_un = @".\unPrivKey.pem";
	static string path_pub_un = @".\unPubKey.pem";
	//Python pem files path
	static string path_priv_py = @".\pyPrivKey.pem";
	static string path_pub_py = @".\pyPubKey.pem";  //Python pem files path

	// Use this for initialization
	void Start()
	{
		//Generate key
		generateKEY(2048);

	}

	// 파이썬 공개키로 암호화
	public byte[] RSAEncryption(byte[] _x){
		RSAParameters pub_key_from_PyPem = loadX509Key (path_pub_py);
		byte[] _encText_py = Encryption(_x, pub_key_from_PyPem, false);
		return _encText_py;
	}

	// 유니티 개인키로 복호화
	public byte[] RSADecryption(byte[] _x){
		RSAParameters priv_key_from_UnPem = loadRSAKey(path_priv_un, true);
		byte[] _decText = Decryption(_x, priv_key_from_UnPem, false);
		return _decText;
	}

	//PEM파일로 부터 RSA 키를 읽어들이는 함수 
	public static RSAParameters loadRSAKey(string path, bool importPrivateKey) {

		var str_pem = System.IO.File.ReadAllText(path);
		RSACryptoServiceProvider tRSA = new RSACryptoServiceProvider();
		if (importPrivateKey == true)
			tRSA = Crypto.DecodeRsaPrivateKey(str_pem);

		else {
			tRSA = Crypto.DecodeRsaPublicKey(str_pem);    
		}
		return (RSAParameters)tRSA.ExportParameters(importPrivateKey);
	}
	//X509 형식화된 PEM파일로 부터 RSA 키를 읽어들이는 함수 
	public static RSAParameters loadX509Key(string path)
	{

		var str_pem = System.IO.File.ReadAllText(path);
		RSACryptoServiceProvider tRSA = Crypto.DecodeX509PublicKey(str_pem);
		return (RSAParameters)tRSA.ExportParameters(false);

	}
	public static void generateKEY(int n) {

		//RSA 키 생성 하는 놈
		RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(n);

		if (!File.Exists(path_priv_un))
		{
			//Private key to PEM format            
			string createText = ExportPrivateKeyToPEMFormat(RSA) + Environment.NewLine;
			File.WriteAllText(path_priv_un, createText);
		}
		if (!File.Exists(path_pub_un))
		{
			//Private key to PEM format           
			string createText = ExportPublicKeyToPEMFormat(RSA) + Environment.NewLine;
			File.WriteAllText(path_pub_un, createText);
		}
	}
	// Update is called once per frame
	void Update()
	{

	}

	public static RSACryptoServiceProvider ImportPublicKey(string pem)
	{
		PemReader pr = new PemReader(new StringReader(pem));
		AsymmetricKeyParameter publicKey = (AsymmetricKeyParameter)pr.ReadObject();
		RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaKeyParameters)publicKey);

		RSACryptoServiceProvider csp = new RSACryptoServiceProvider();// cspParams);
		csp.ImportParameters(rsaParams);
		return csp;
	}

	public static RSACryptoServiceProvider DecodeRSAPrivateKey(byte[] privkey)
	{
		byte[] MODULUS, E, D, P, Q, DP, DQ, IQ;

		// ---------  Set up stream to decode the asn.1 encoded RSA private key  ------
		MemoryStream mem = new MemoryStream(privkey);
		BinaryReader binr = new BinaryReader(mem);    //wrap Memory Stream with BinaryReader for easy reading
		byte bt = 0;
		ushort twobytes = 0;
		int elems = 0;
		try
		{
			twobytes = binr.ReadUInt16();
			if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
				binr.ReadByte();        //advance 1 byte
			else if (twobytes == 0x8230)
				binr.ReadInt16();       //advance 2 bytes
			else
				return null;

			twobytes = binr.ReadUInt16();
			if (twobytes != 0x0102) //version number
				return null;
			bt = binr.ReadByte();
			if (bt != 0x00)
				return null;


			//------  all private key components are Integer sequences ----
			elems = GetIntegerSize(binr);
			MODULUS = binr.ReadBytes(elems);

			elems = GetIntegerSize(binr);
			E = binr.ReadBytes(elems);

			elems = GetIntegerSize(binr);
			D = binr.ReadBytes(elems);

			elems = GetIntegerSize(binr);
			P = binr.ReadBytes(elems);

			elems = GetIntegerSize(binr);
			Q = binr.ReadBytes(elems);

			elems = GetIntegerSize(binr);
			DP = binr.ReadBytes(elems);

			elems = GetIntegerSize(binr);
			DQ = binr.ReadBytes(elems);

			elems = GetIntegerSize(binr);
			IQ = binr.ReadBytes(elems);

			Console.WriteLine("showing components ..");
			if (verbose)
			{
				showBytes("\nModulus", MODULUS);
				showBytes("\nExponent", E);
				showBytes("\nD", D);
				showBytes("\nP", P);
				showBytes("\nQ", Q);
				showBytes("\nDP", DP);
				showBytes("\nDQ", DQ);
				showBytes("\nIQ", IQ);
			}

			// ------- create RSACryptoServiceProvider instance and initialize with public key -----
			RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
			RSAParameters RSAparams = new RSAParameters();
			RSAparams.Modulus = MODULUS;
			RSAparams.Exponent = E;
			RSAparams.D = D;
			RSAparams.P = P;
			RSAparams.Q = Q;
			RSAparams.DP = DP;
			RSAparams.DQ = DQ;
			RSAparams.InverseQ = IQ;
			RSA.ImportParameters(RSAparams);
			return RSA;
		}
		catch (Exception)
		{
			return null;
		}
		finally
		{
			binr.Close();
		}
	}

	private static int GetIntegerSize(BinaryReader binr)
	{
		byte bt = 0;
		byte lowbyte = 0x00;
		byte highbyte = 0x00;
		int count = 0;
		bt = binr.ReadByte();
		if (bt != 0x02)     //expect integer
			return 0;
		bt = binr.ReadByte();

		if (bt == 0x81)
			count = binr.ReadByte();    // data size in next byte
		else
			if (bt == 0x82)
			{
				highbyte = binr.ReadByte(); // data size in next 2 bytes
				lowbyte = binr.ReadByte();
				byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
				count = BitConverter.ToInt32(modint, 0);
			}
			else
			{
				count = bt;     // we already have the data size
			}

		while (binr.ReadByte() == 0x00)
		{   //remove high order zeros in data
			count -= 1;
		}
		binr.BaseStream.Seek(-1, SeekOrigin.Current);       //last ReadByte wasn't a removed zero, so back up a byte
		return count;
	}

	private static void showBytes(String info, byte[] data)
	{
		Console.WriteLine("{0} [{1} bytes]", info, data.Length);
		for (int i = 1; i <= data.Length; i++)
		{
			Console.Write("{0:X2} ", data[i - 1]);
			if (i % 16 == 0)
				Console.WriteLine();
		}
		Console.WriteLine("\n\n");
	}


	private string GetStringFromPEM(string pemString, string section)
	{
		var header = String.Format("-----BEGIN {0}-----", section);
		var footer = String.Format("-----END {0}-----", section);


		var start = pemString.IndexOf(header, StringComparison.Ordinal);
		if (start < 0)
			return null;

		start += header.Length;
		var end = pemString.IndexOf(footer, start, StringComparison.Ordinal) - start;

		if (end < 0)
			return null;

		return pemString.Substring(start, end).Trim();

	}

	private byte[] GetBytesFromPEM(string pemString, string section)
	{
		var header = String.Format("-----BEGIN {0}-----", section);
		var footer = String.Format("-----END {0}-----", section);

		var start = pemString.IndexOf(header, StringComparison.Ordinal);
		if (start < 0)
			return null;

		start += header.Length;
		var end = pemString.IndexOf(footer, start, StringComparison.Ordinal) - start;

		if (end < 0)
			return null;

		return ByteConv.GetBytes(pemString.Substring(start, end).Trim());
	}

	static public byte[] Encryption(byte[] Data, RSAParameters RSAKey, bool DoOAEPPadding)
	{
		try
		{
			byte[] encryptedData;
			using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
			{
				RSA.ImportParameters(RSAKey);
				encryptedData = RSA.Encrypt(Data, DoOAEPPadding);
			}
			return encryptedData;
		}
		catch (CryptographicException e)
		{
			Console.WriteLine(e.Message);
			return null;
		}
	}

	static public byte[] Decryption(byte[] Data, RSAParameters RSAKey, bool DoOAEPPadding)
	{
		try
		{
			byte[] decryptedData;
			using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
			{
				RSA.ImportParameters(RSAKey);
				decryptedData = RSA.Decrypt(Data, DoOAEPPadding);
			}
			return decryptedData;
		}
		catch (CryptographicException e)
		{
			Console.WriteLine(e.ToString());
			return null;
		}
	}


	private static string ExportPrivateKeyToPEMFormat(RSACryptoServiceProvider csp)
	{
		TextWriter outputStream = new StringWriter();

		if (csp.PublicOnly) throw new ArgumentException("CSP does not contain a private key", "csp");
		var parameters = csp.ExportParameters(true);
		using (var stream = new MemoryStream())
		{
			var writer = new BinaryWriter(stream);
			writer.Write((byte)0x30); // SEQUENCE
			using (var innerStream = new MemoryStream())
			{
				var innerWriter = new BinaryWriter(innerStream);
				EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
				EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
				EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
				EncodeIntegerBigEndian(innerWriter, parameters.D);
				EncodeIntegerBigEndian(innerWriter, parameters.P);
				EncodeIntegerBigEndian(innerWriter, parameters.Q);
				EncodeIntegerBigEndian(innerWriter, parameters.DP);
				EncodeIntegerBigEndian(innerWriter, parameters.DQ);
				EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
				var length = (int)innerStream.Length;
				EncodeLength(writer, length);
				writer.Write(innerStream.GetBuffer(), 0, length);
			}

			var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
			outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
			// Output as Base64 with lines chopped at 64 characters
			for (var i = 0; i < base64.Length; i += 64)
			{
				outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
			}
			outputStream.WriteLine("-----END RSA PRIVATE KEY-----");
			return outputStream.ToString();
		}
	}

	public static String ExportPublicKeyToPEMFormat(RSACryptoServiceProvider csp)
	{
		TextWriter outputStream = new StringWriter();

		var parameters = csp.ExportParameters(false);
		using (var stream = new MemoryStream())
		{
			var writer = new BinaryWriter(stream);
			writer.Write((byte)0x30); // SEQUENCE
			using (var innerStream = new MemoryStream())
			{
				var innerWriter = new BinaryWriter(innerStream);
				EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
				EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
				EncodeIntegerBigEndian(innerWriter, parameters.Exponent);

				//All Parameter Must Have Value so Set Other Parameter Value Whit Invalid Data  (for keeping Key Structure  use "parameters.Exponent" value for invalid data)
				EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.D
				EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.P
				EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.Q
				EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DP
				EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DQ
				EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.InverseQ

				var length = (int)innerStream.Length;
				EncodeLength(writer, length);
				writer.Write(innerStream.GetBuffer(), 0, length);
			}

			var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
			outputStream.WriteLine("-----BEGIN PUBLIC KEY-----");
			// Output as Base64 with lines chopped at 64 characters
			for (var i = 0; i < base64.Length; i += 64)
			{
				outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
			}
			outputStream.WriteLine("-----END PUBLIC KEY-----");

			return outputStream.ToString();

		}
	}

	private static void EncodeLength(BinaryWriter stream, int length)
	{
		if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
		if (length < 0x80)
		{
			// Short form
			stream.Write((byte)length);
		}
		else
		{
			// Long form
			var temp = length;
			var bytesRequired = 0;
			while (temp > 0)
			{
				temp >>= 8;
				bytesRequired++;
			}
			stream.Write((byte)(bytesRequired | 0x80));
			for (var i = bytesRequired - 1; i >= 0; i--)
			{
				stream.Write((byte)(length >> (8 * i) & 0xff));
			}
		}
	}

	private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
	{
		stream.Write((byte)0x02); // INTEGER
		var prefixZeros = 0;
		for (var i = 0; i < value.Length; i++)
		{
			if (value[i] != 0) break;
			prefixZeros++;
		}
		if (value.Length - prefixZeros == 0)
		{
			EncodeLength(stream, 1);
			stream.Write((byte)0);
		}
		else
		{
			if (forceUnsigned && value[prefixZeros] > 0x7f)
			{
				// Add a prefix zero to force unsigned if the MSB is 1
				EncodeLength(stream, value.Length - prefixZeros + 1);
				stream.Write((byte)0);
			}
			else
			{
				EncodeLength(stream, value.Length - prefixZeros);
			}
			for (var i = prefixZeros; i < value.Length; i++)
			{
				stream.Write(value[i]);
			}
		}
	}

	private static string ByteArrayToString(byte[] val)
	{
		string b = "";
		int len = val.Length;
		for(int i = 0; i < len; i++) {
			if(i != 0) {
				b += ",";
			}            
			b += val[i].ToString();
		}
		return b;
	}

}