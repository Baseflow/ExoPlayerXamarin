using System;
using System.Collections.Generic;
using Android.Runtime;

// IExoMediaDrm.SetOnEventListener was automatically implemented by Bindings Generator -
// generated implementation tried to call IExoMediaDrmOnEventListener<T> (..but generic version of this interface does not exist)
// As I was not able to remove generated method I removed node, copied generated code and fixed issue by hand.
// @thefex

namespace Com.Google.Android.Exoplayer.Drm {

	// Metadata.xml XPath class reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']"
	[global::Android.Runtime.Register ("com/google/android/exoplayer/drm/FrameworkMediaDrm", DoNotGenerateAcw=true)]
	public sealed partial class FrameworkMediaDrm : global::Java.Lang.Object, global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrm {

		internal static IntPtr java_class_handle;
		internal static IntPtr class_ref {
			get {
				return JNIEnv.FindClass ("com/google/android/exoplayer/drm/FrameworkMediaDrm", ref java_class_handle);
			}
		}

		protected override IntPtr ThresholdClass {
			get { return class_ref; }
		}

		protected override global::System.Type ThresholdType {
			get { return typeof (FrameworkMediaDrm); }
		}

		internal FrameworkMediaDrm (IntPtr javaReference, JniHandleOwnership transfer) : base (javaReference, transfer) {}

		static IntPtr id_ctor_Ljava_util_UUID_;
		// Metadata.xml XPath constructor reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/constructor[@name='FrameworkMediaDrm' and count(parameter)=1 and parameter[1][@type='java.util.UUID']]"
		[Register (".ctor", "(Ljava/util/UUID;)V", "")]
		public unsafe FrameworkMediaDrm (global::Java.Util.UUID p0)
			: base (IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			if (((global::Java.Lang.Object) this).Handle != IntPtr.Zero)
				return;

			try {
				JValue* __args = stackalloc JValue [1];
				__args [0] = new JValue (p0);
				if (GetType () != typeof (FrameworkMediaDrm)) {
					SetHandle (
							global::Android.Runtime.JNIEnv.StartCreateInstance (GetType (), "(Ljava/util/UUID;)V", __args),
							JniHandleOwnership.TransferLocalRef);
					global::Android.Runtime.JNIEnv.FinishCreateInstance (((global::Java.Lang.Object) this).Handle, "(Ljava/util/UUID;)V", __args);
					return;
				}

				if (id_ctor_Ljava_util_UUID_ == IntPtr.Zero)
					id_ctor_Ljava_util_UUID_ = JNIEnv.GetMethodID (class_ref, "<init>", "(Ljava/util/UUID;)V");
				SetHandle (
						global::Android.Runtime.JNIEnv.StartCreateInstance (class_ref, id_ctor_Ljava_util_UUID_, __args),
						JniHandleOwnership.TransferLocalRef);
				JNIEnv.FinishCreateInstance (((global::Java.Lang.Object) this).Handle, class_ref, id_ctor_Ljava_util_UUID_, __args);
			} finally {
			}
		}

		static IntPtr id_getProvisionRequest;
		public unsafe global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrmProvisionRequest ProvisionRequest {
			// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='getProvisionRequest' and count(parameter)=0]"
			[Register ("getProvisionRequest", "()Lcom/google/android/exoplayer/drm/ExoMediaDrm$ProvisionRequest;", "GetGetProvisionRequestHandler")]
			get {
				if (id_getProvisionRequest == IntPtr.Zero)
					id_getProvisionRequest = JNIEnv.GetMethodID (class_ref, "getProvisionRequest", "()Lcom/google/android/exoplayer/drm/ExoMediaDrm$ProvisionRequest;");
				try {
					return global::Java.Lang.Object.GetObject<global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrmProvisionRequest> (JNIEnv.CallObjectMethod (((global::Java.Lang.Object) this).Handle, id_getProvisionRequest), JniHandleOwnership.TransferLocalRef);
				} finally {
				}
			}
		}

		static IntPtr id_closeSession_arrayB;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='closeSession' and count(parameter)=1 and parameter[1][@type='byte[]']]"
		[Register ("closeSession", "([B)V", "")]
		public unsafe void CloseSession (byte[] p0)
		{
			if (id_closeSession_arrayB == IntPtr.Zero)
				id_closeSession_arrayB = JNIEnv.GetMethodID (class_ref, "closeSession", "([B)V");
			IntPtr native_p0 = JNIEnv.NewArray (p0);
			try {
				JValue* __args = stackalloc JValue [1];
				__args [0] = new JValue (native_p0);
				JNIEnv.CallVoidMethod (((global::Java.Lang.Object) this).Handle, id_closeSession_arrayB, __args);
			} finally {
				if (p0 != null) {
					JNIEnv.CopyArray (native_p0, p0);
					JNIEnv.DeleteLocalRef (native_p0);
				}
			}
		}

		static IntPtr id_createMediaCrypto_Ljava_util_UUID_arrayB;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='createMediaCrypto' and count(parameter)=2 and parameter[1][@type='java.util.UUID'] and parameter[2][@type='byte[]']]"
		[Register ("createMediaCrypto", "(Ljava/util/UUID;[B)Lcom/google/android/exoplayer/drm/FrameworkMediaCrypto;", "")]
		public unsafe global::Com.Google.Android.Exoplayer.Drm.FrameworkMediaCrypto CreateMediaCrypto (global::Java.Util.UUID p0, byte[] p1)
		{
			if (id_createMediaCrypto_Ljava_util_UUID_arrayB == IntPtr.Zero)
				id_createMediaCrypto_Ljava_util_UUID_arrayB = JNIEnv.GetMethodID (class_ref, "createMediaCrypto", "(Ljava/util/UUID;[B)Lcom/google/android/exoplayer/drm/FrameworkMediaCrypto;");
			IntPtr native_p1 = JNIEnv.NewArray (p1);
			try {
				JValue* __args = stackalloc JValue [2];
				__args [0] = new JValue (p0);
				__args [1] = new JValue (native_p1);
				global::Com.Google.Android.Exoplayer.Drm.FrameworkMediaCrypto __ret = global::Java.Lang.Object.GetObject<global::Com.Google.Android.Exoplayer.Drm.FrameworkMediaCrypto> (JNIEnv.CallObjectMethod (((global::Java.Lang.Object) this).Handle, id_createMediaCrypto_Ljava_util_UUID_arrayB, __args), JniHandleOwnership.TransferLocalRef);
				return __ret;
			} finally {
				if (p1 != null) {
					JNIEnv.CopyArray (native_p1, p1);
					JNIEnv.DeleteLocalRef (native_p1);
				}
			}
		}

		static IntPtr id_getKeyRequest_arrayBarrayBLjava_lang_String_ILjava_util_HashMap_;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='getKeyRequest' and count(parameter)=5 and parameter[1][@type='byte[]'] and parameter[2][@type='byte[]'] and parameter[3][@type='java.lang.String'] and parameter[4][@type='int'] and parameter[5][@type='java.util.HashMap&lt;java.lang.String, java.lang.String&gt;']]"
		[Register ("getKeyRequest", "([B[BLjava/lang/String;ILjava/util/HashMap;)Lcom/google/android/exoplayer/drm/ExoMediaDrm$KeyRequest;", "")]
		public unsafe global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrmKeyRequest GetKeyRequest (byte[] p0, byte[] p1, string p2, int p3, global::System.Collections.Generic.IDictionary<string, string> p4)
		{
			if (id_getKeyRequest_arrayBarrayBLjava_lang_String_ILjava_util_HashMap_ == IntPtr.Zero)
				id_getKeyRequest_arrayBarrayBLjava_lang_String_ILjava_util_HashMap_ = JNIEnv.GetMethodID (class_ref, "getKeyRequest", "([B[BLjava/lang/String;ILjava/util/HashMap;)Lcom/google/android/exoplayer/drm/ExoMediaDrm$KeyRequest;");
			IntPtr native_p0 = JNIEnv.NewArray (p0);
			IntPtr native_p1 = JNIEnv.NewArray (p1);
			IntPtr native_p2 = JNIEnv.NewString (p2);
			IntPtr native_p4 = global::Android.Runtime.JavaDictionary<string, string>.ToLocalJniHandle (p4);
			try {
				JValue* __args = stackalloc JValue [5];
				__args [0] = new JValue (native_p0);
				__args [1] = new JValue (native_p1);
				__args [2] = new JValue (native_p2);
				__args [3] = new JValue (p3);
				__args [4] = new JValue (native_p4);
				global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrmKeyRequest __ret = global::Java.Lang.Object.GetObject<global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrmKeyRequest> (JNIEnv.CallObjectMethod (((global::Java.Lang.Object) this).Handle, id_getKeyRequest_arrayBarrayBLjava_lang_String_ILjava_util_HashMap_, __args), JniHandleOwnership.TransferLocalRef);
				return __ret;
			} finally {
				if (p0 != null) {
					JNIEnv.CopyArray (native_p0, p0);
					JNIEnv.DeleteLocalRef (native_p0);
				}
				if (p1 != null) {
					JNIEnv.CopyArray (native_p1, p1);
					JNIEnv.DeleteLocalRef (native_p1);
				}
				JNIEnv.DeleteLocalRef (native_p2);
				JNIEnv.DeleteLocalRef (native_p4);
			}
		}

		static IntPtr id_getPropertyByteArray_Ljava_lang_String_;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='getPropertyByteArray' and count(parameter)=1 and parameter[1][@type='java.lang.String']]"
		[Register ("getPropertyByteArray", "(Ljava/lang/String;)[B", "")]
		public unsafe byte[] GetPropertyByteArray (string p0)
		{
			if (id_getPropertyByteArray_Ljava_lang_String_ == IntPtr.Zero)
				id_getPropertyByteArray_Ljava_lang_String_ = JNIEnv.GetMethodID (class_ref, "getPropertyByteArray", "(Ljava/lang/String;)[B");
			IntPtr native_p0 = JNIEnv.NewString (p0);
			try {
				JValue* __args = stackalloc JValue [1];
				__args [0] = new JValue (native_p0);
				byte[] __ret = (byte[]) JNIEnv.GetArray (JNIEnv.CallObjectMethod (((global::Java.Lang.Object) this).Handle, id_getPropertyByteArray_Ljava_lang_String_, __args), JniHandleOwnership.TransferLocalRef, typeof (byte));
				return __ret;
			} finally {
				JNIEnv.DeleteLocalRef (native_p0);
			}
		}

		static IntPtr id_getPropertyString_Ljava_lang_String_;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='getPropertyString' and count(parameter)=1 and parameter[1][@type='java.lang.String']]"
		[Register ("getPropertyString", "(Ljava/lang/String;)Ljava/lang/String;", "")]
		public unsafe string GetPropertyString (string p0)
		{
			if (id_getPropertyString_Ljava_lang_String_ == IntPtr.Zero)
				id_getPropertyString_Ljava_lang_String_ = JNIEnv.GetMethodID (class_ref, "getPropertyString", "(Ljava/lang/String;)Ljava/lang/String;");
			IntPtr native_p0 = JNIEnv.NewString (p0);
			try {
				JValue* __args = stackalloc JValue [1];
				__args [0] = new JValue (native_p0);
				string __ret = JNIEnv.GetString (JNIEnv.CallObjectMethod (((global::Java.Lang.Object) this).Handle, id_getPropertyString_Ljava_lang_String_, __args), JniHandleOwnership.TransferLocalRef);
				return __ret;
			} finally {
				JNIEnv.DeleteLocalRef (native_p0);
			}
		}

		static IntPtr id_openSession;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='openSession' and count(parameter)=0]"
		[Register ("openSession", "()[B", "")]
		public unsafe byte[] OpenSession ()
		{
			if (id_openSession == IntPtr.Zero)
				id_openSession = JNIEnv.GetMethodID (class_ref, "openSession", "()[B");
			try {
				return (byte[]) JNIEnv.GetArray (JNIEnv.CallObjectMethod (((global::Java.Lang.Object) this).Handle, id_openSession), JniHandleOwnership.TransferLocalRef, typeof (byte));
			} finally {
			}
		}

		static IntPtr id_provideKeyResponse_arrayBarrayB;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='provideKeyResponse' and count(parameter)=2 and parameter[1][@type='byte[]'] and parameter[2][@type='byte[]']]"
		[Register ("provideKeyResponse", "([B[B)[B", "")]
		public unsafe byte[] ProvideKeyResponse (byte[] p0, byte[] p1)
		{
			if (id_provideKeyResponse_arrayBarrayB == IntPtr.Zero)
				id_provideKeyResponse_arrayBarrayB = JNIEnv.GetMethodID (class_ref, "provideKeyResponse", "([B[B)[B");
			IntPtr native_p0 = JNIEnv.NewArray (p0);
			IntPtr native_p1 = JNIEnv.NewArray (p1);
			try {
				JValue* __args = stackalloc JValue [2];
				__args [0] = new JValue (native_p0);
				__args [1] = new JValue (native_p1);
				byte[] __ret = (byte[]) JNIEnv.GetArray (JNIEnv.CallObjectMethod (((global::Java.Lang.Object) this).Handle, id_provideKeyResponse_arrayBarrayB, __args), JniHandleOwnership.TransferLocalRef, typeof (byte));
				return __ret;
			} finally {
				if (p0 != null) {
					JNIEnv.CopyArray (native_p0, p0);
					JNIEnv.DeleteLocalRef (native_p0);
				}
				if (p1 != null) {
					JNIEnv.CopyArray (native_p1, p1);
					JNIEnv.DeleteLocalRef (native_p1);
				}
			}
		}

		static IntPtr id_provideProvisionResponse_arrayB;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='provideProvisionResponse' and count(parameter)=1 and parameter[1][@type='byte[]']]"
		[Register ("provideProvisionResponse", "([B)V", "")]
		public unsafe void ProvideProvisionResponse (byte[] p0)
		{
			if (id_provideProvisionResponse_arrayB == IntPtr.Zero)
				id_provideProvisionResponse_arrayB = JNIEnv.GetMethodID (class_ref, "provideProvisionResponse", "([B)V");
			IntPtr native_p0 = JNIEnv.NewArray (p0);
			try {
				JValue* __args = stackalloc JValue [1];
				__args [0] = new JValue (native_p0);
				JNIEnv.CallVoidMethod (((global::Java.Lang.Object) this).Handle, id_provideProvisionResponse_arrayB, __args);
			} finally {
				if (p0 != null) {
					JNIEnv.CopyArray (native_p0, p0);
					JNIEnv.DeleteLocalRef (native_p0);
				}
			}
		}

		static IntPtr id_queryKeyStatus_arrayB;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='queryKeyStatus' and count(parameter)=1 and parameter[1][@type='byte[]']]"
		[Register ("queryKeyStatus", "([B)Ljava/util/Map;", "")]
		public unsafe global::System.Collections.Generic.IDictionary<string, string> QueryKeyStatus (byte[] p0)
		{
			if (id_queryKeyStatus_arrayB == IntPtr.Zero)
				id_queryKeyStatus_arrayB = JNIEnv.GetMethodID (class_ref, "queryKeyStatus", "([B)Ljava/util/Map;");
			IntPtr native_p0 = JNIEnv.NewArray (p0);
			try {
				JValue* __args = stackalloc JValue [1];
				__args [0] = new JValue (native_p0);
				global::System.Collections.Generic.IDictionary<string, string> __ret = global::Android.Runtime.JavaDictionary<string, string>.FromJniHandle (JNIEnv.CallObjectMethod (((global::Java.Lang.Object) this).Handle, id_queryKeyStatus_arrayB, __args), JniHandleOwnership.TransferLocalRef);
				return __ret;
			} finally {
				if (p0 != null) {
					JNIEnv.CopyArray (native_p0, p0);
					JNIEnv.DeleteLocalRef (native_p0);
				}
			}
		}

		static IntPtr id_release;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='release' and count(parameter)=0]"
		[Register ("release", "()V", "")]
		public unsafe void Release ()
		{
			if (id_release == IntPtr.Zero)
				id_release = JNIEnv.GetMethodID (class_ref, "release", "()V");
			try {
				JNIEnv.CallVoidMethod (((global::Java.Lang.Object) this).Handle, id_release);
			} finally {
			}
		}

		static IntPtr id_restoreKeys_arrayBarrayB;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='restoreKeys' and count(parameter)=2 and parameter[1][@type='byte[]'] and parameter[2][@type='byte[]']]"
		[Register ("restoreKeys", "([B[B)V", "")]
		public unsafe void RestoreKeys (byte[] p0, byte[] p1)
		{
			if (id_restoreKeys_arrayBarrayB == IntPtr.Zero)
				id_restoreKeys_arrayBarrayB = JNIEnv.GetMethodID (class_ref, "restoreKeys", "([B[B)V");
			IntPtr native_p0 = JNIEnv.NewArray (p0);
			IntPtr native_p1 = JNIEnv.NewArray (p1);
			try {
				JValue* __args = stackalloc JValue [2];
				__args [0] = new JValue (native_p0);
				__args [1] = new JValue (native_p1);
				JNIEnv.CallVoidMethod (((global::Java.Lang.Object) this).Handle, id_restoreKeys_arrayBarrayB, __args);
			} finally {
				if (p0 != null) {
					JNIEnv.CopyArray (native_p0, p0);
					JNIEnv.DeleteLocalRef (native_p0);
				}
				if (p1 != null) {
					JNIEnv.CopyArray (native_p1, p1);
					JNIEnv.DeleteLocalRef (native_p1);
				}
			}
		}

		static IntPtr id_setOnEventListener_Lcom_google_android_exoplayer_drm_ExoMediaDrm_OnEventListener_;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='setOnEventListener' and count(parameter)=1 and parameter[1][@type='com.google.android.exoplayer.drm.ExoMediaDrm.OnEventListener&lt;? super com.google.android.exoplayer.drm.FrameworkMediaCrypto&gt;']]"
		[Register ("setOnEventListener", "(Lcom/google/android/exoplayer/drm/ExoMediaDrm$OnEventListener;)V", "")]
		public unsafe void SetOnEventListener (global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrmOnEventListener p0)
		{
			if (id_setOnEventListener_Lcom_google_android_exoplayer_drm_ExoMediaDrm_OnEventListener_ == IntPtr.Zero)
				id_setOnEventListener_Lcom_google_android_exoplayer_drm_ExoMediaDrm_OnEventListener_ = JNIEnv.GetMethodID (class_ref, "setOnEventListener", "(Lcom/google/android/exoplayer/drm/ExoMediaDrm$OnEventListener;)V");
			try {
				JValue* __args = stackalloc JValue [1];
				__args [0] = new JValue (p0);
				JNIEnv.CallVoidMethod (((global::Java.Lang.Object) this).Handle, id_setOnEventListener_Lcom_google_android_exoplayer_drm_ExoMediaDrm_OnEventListener_, __args);
			} finally {
			}
		}

		static IntPtr id_setPropertyByteArray_Ljava_lang_String_arrayB;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='setPropertyByteArray' and count(parameter)=2 and parameter[1][@type='java.lang.String'] and parameter[2][@type='byte[]']]"
		[Register ("setPropertyByteArray", "(Ljava/lang/String;[B)V", "")]
		public unsafe void SetPropertyByteArray (string p0, byte[] p1)
		{
			if (id_setPropertyByteArray_Ljava_lang_String_arrayB == IntPtr.Zero)
				id_setPropertyByteArray_Ljava_lang_String_arrayB = JNIEnv.GetMethodID (class_ref, "setPropertyByteArray", "(Ljava/lang/String;[B)V");
			IntPtr native_p0 = JNIEnv.NewString (p0);
			IntPtr native_p1 = JNIEnv.NewArray (p1);
			try {
				JValue* __args = stackalloc JValue [2];
				__args [0] = new JValue (native_p0);
				__args [1] = new JValue (native_p1);
				JNIEnv.CallVoidMethod (((global::Java.Lang.Object) this).Handle, id_setPropertyByteArray_Ljava_lang_String_arrayB, __args);
			} finally {
				JNIEnv.DeleteLocalRef (native_p0);
				if (p1 != null) {
					JNIEnv.CopyArray (native_p1, p1);
					JNIEnv.DeleteLocalRef (native_p1);
				}
			}
		}

		static IntPtr id_setPropertyString_Ljava_lang_String_Ljava_lang_String_;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer.drm']/class[@name='FrameworkMediaDrm']/method[@name='setPropertyString' and count(parameter)=2 and parameter[1][@type='java.lang.String'] and parameter[2][@type='java.lang.String']]"
		[Register ("setPropertyString", "(Ljava/lang/String;Ljava/lang/String;)V", "")]
		public unsafe void SetPropertyString (string p0, string p1)
		{
			if (id_setPropertyString_Ljava_lang_String_Ljava_lang_String_ == IntPtr.Zero)
				id_setPropertyString_Ljava_lang_String_Ljava_lang_String_ = JNIEnv.GetMethodID (class_ref, "setPropertyString", "(Ljava/lang/String;Ljava/lang/String;)V");
			IntPtr native_p0 = JNIEnv.NewString (p0);
			IntPtr native_p1 = JNIEnv.NewString (p1);
			try {
				JValue* __args = stackalloc JValue [2];
				__args [0] = new JValue (native_p0);
				__args [1] = new JValue (native_p1);
				JNIEnv.CallVoidMethod (((global::Java.Lang.Object) this).Handle, id_setPropertyString_Ljava_lang_String_Ljava_lang_String_, __args);
			} finally {
				JNIEnv.DeleteLocalRef (native_p0);
				JNIEnv.DeleteLocalRef (native_p1);
			}
		}

		// This method is explicitly implemented as a member of an instantiated Com.Google.Android.Exoplayer.Drm.IExoMediaDrm
		global::Java.Lang.Object global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrm.CreateMediaCrypto (global::Java.Util.UUID p0, byte[] p1)
		{
			return global::Java.Interop.JavaObjectExtensions.JavaCast<Java.Lang.Object>(CreateMediaCrypto (p0, p1));
		}

		// This method is explicitly implemented as a member of an instantiated Com.Google.Android.Exoplayer.Drm.IExoMediaDrm
		void global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrm.SetOnEventListener (global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrmOnEventListener p0)
		{
			SetOnEventListener (global::Java.Interop.JavaObjectExtensions.JavaCast<global::Com.Google.Android.Exoplayer.Drm.IExoMediaDrmOnEventListener>(p0));
		}

	}
}
