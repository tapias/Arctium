using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Native
{
    public class NativeMethods
    {
        [DllImport("Libeay32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EVP_CIPHER_CTX_init(ref EVP_CTX ctx);

        [DllImport("Libeay32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EVP_EncryptInit_ex(ref EVP_CTX ctx, IntPtr Cipher, IntPtr Engine, byte[] key, byte[] iv);

        [DllImport("Libeay32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CIPHER_CTX_set_key_length(ref EVP_CTX ctx, int keylen);

        [DllImport("Libeay32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CIPHER_CTX_cleanup(ref EVP_CTX ctx);

        [DllImport("Libeay32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_EncryptUpdate(ref EVP_CTX ctx, [Out] byte[] outp, ref int outL, [In] byte[] inp, int inplen);

        [DllImport("Libeay32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_EncryptFinal_ex(ref EVP_CTX ctx, [Out] byte[] output, ref int outL);

        [DllImport("Libeay32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr EVP_rc4();

        [DllImport("libeay32.dll")]
        internal static extern int BN_add(IntPtr r, IntPtr a, IntPtr b);

        [DllImport("libeay32.dll", EntryPoint = "BN_bin2bn")]
        internal static extern IntPtr BN_Bin2BN(byte[] ByteArrayIn, int length, IntPtr to);

        [DllImport("libeay32.dll")]
        internal static extern int BN_bn2bin(IntPtr a, byte[] to);

        [DllImport("libeay32.dll", EntryPoint = "BN_CTX_free")]
        internal static extern int BN_ctx_free(IntPtr a);

        [DllImport("libeay32.dll", EntryPoint = "BN_CTX_new")]
        internal static extern IntPtr BN_ctx_new();

        [DllImport("libeay32.dll")]
        internal static extern int BN_div(IntPtr dv, IntPtr r, IntPtr a, IntPtr b, IntPtr ctx);

        [DllImport("libeay32.dll", EntryPoint = "BN_free")]
        internal static extern void BN_Free(IntPtr r);

        [DllImport("libeay32.dll")]
        internal static extern IntPtr BN_mod_exp(IntPtr res, IntPtr a, IntPtr p, IntPtr m, IntPtr ctx);

        [DllImport("libeay32.dll")]
        internal static extern int BN_mul(IntPtr r, IntPtr a, IntPtr b, IntPtr ctx);

        [DllImport("libeay32.dll", EntryPoint = "BN_new")]
        internal static extern IntPtr BN_New();

        [DllImport("libeay32.dll")]
        public static extern int RAND_bytes(byte[] buf, int num);
    }
}
