using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CustomActions.Helpers
{
	internal class Crypto
	{
		/// <summary>
		/// This helper function parses an RSA private key using the ASN.1 format
		/// </summary>
		/// <param name="privateKeyBytes">Byte array containing PEM string of private key.</param>
		/// <returns>An instance of <see cref="RSACryptoServiceProvider"/> rapresenting the requested private key.
		/// Null if method fails on retriving the key.</returns>
		public static RSACryptoServiceProvider DecodeRsaPrivateKey(byte[] privateKeyBytes)
		{
			MemoryStream ms = new MemoryStream(privateKeyBytes);
			BinaryReader rd = new BinaryReader(ms);

			try
			{
				byte byteValue;
				ushort shortValue;

				shortValue = rd.ReadUInt16();

				switch (shortValue)
				{
					case 0x8130:
						// If true, data is little endian since the proper logical seq is 0x30 0x81
						rd.ReadByte(); //advance 1 byte
						break;
					case 0x8230:
						rd.ReadInt16();  //advance 2 bytes
						break;
					default:
						Debug.Assert(false);     // Improper ASN.1 format
						return null;
				}

				shortValue = rd.ReadUInt16();
				if (shortValue != 0x0102) // (version number)
				{
					Debug.Assert(false);     // Improper ASN.1 format, unexpected version number
					return null;
				}

				byteValue = rd.ReadByte();
				if (byteValue != 0x00)
				{
					Debug.Assert(false);     // Improper ASN.1 format
					return null;
				}

				// The data following the version will be the ASN.1 data itself, which in our case
				// are a sequence of integers.

				// In order to solve a problem with instancing RSACryptoServiceProvider
				// via default constructor on .net 4.0 this is a hack
				CspParameters parms = new CspParameters();
				parms.Flags = CspProviderFlags.NoFlags;
				parms.KeyContainerName = Guid.NewGuid().ToString().ToUpperInvariant();
				parms.ProviderType = ((Environment.OSVersion.Version.Major > 5) || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1))) ? 0x18 : 1;

				RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(parms);
				RSAParameters rsAparams = new RSAParameters();

				rsAparams.Modulus = rd.ReadBytes(HelperFunctions.DecodeIntegerSize(rd));

				// Argh, this is a pain.  From emperical testing it appears to be that RSAParameters doesn't like byte buffers that
				// have their leading zeros removed.  The RFC doesn't address this area that I can see, so it's hard to say that this
				// is a bug, but it sure would be helpful if it allowed that. So, there's some extra code here that knows what the
				// sizes of the various components are supposed to be.  Using these sizes we can ensure the buffer sizes are exactly
				// what the RSAParameters expect.  Thanks, Microsoft.
				RSAParameterTraits traits = new RSAParameterTraits(rsAparams.Modulus.Length * 8);

				rsAparams.Modulus = HelperFunctions.AlignBytes(rsAparams.Modulus, traits.size_Mod);
				rsAparams.Exponent = HelperFunctions.AlignBytes(rd.ReadBytes(HelperFunctions.DecodeIntegerSize(rd)), traits.size_Exp);
				rsAparams.D = HelperFunctions.AlignBytes(rd.ReadBytes(HelperFunctions.DecodeIntegerSize(rd)), traits.size_D);
				rsAparams.P = HelperFunctions.AlignBytes(rd.ReadBytes(HelperFunctions.DecodeIntegerSize(rd)), traits.size_P);
				rsAparams.Q = HelperFunctions.AlignBytes(rd.ReadBytes(HelperFunctions.DecodeIntegerSize(rd)), traits.size_Q);
				rsAparams.DP = HelperFunctions.AlignBytes(rd.ReadBytes(HelperFunctions.DecodeIntegerSize(rd)), traits.size_DP);
				rsAparams.DQ = HelperFunctions.AlignBytes(rd.ReadBytes(HelperFunctions.DecodeIntegerSize(rd)), traits.size_DQ);
				rsAparams.InverseQ = HelperFunctions.AlignBytes(rd.ReadBytes(HelperFunctions.DecodeIntegerSize(rd)), traits.size_InvQ);

				rsa.ImportParameters(rsAparams);
				return rsa;
			}
			catch (Exception)
			{
				Debug.Assert(false);
				return null;
			}
			finally
			{
				rd.Close();
			}
		}
	}
}
