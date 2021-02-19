
using Android.App;
using Android.Content;

namespace CloudStreamForms.Droid
{
	// Thanks to https://github.com/MrClockOff/DocumentPicker/blob/a063750a60c6e2c72ddd3026244c50148e715d12/DocumentPickerTest.Droid/FileContentProvider.cs

	[ContentProvider(new[] { "@PACKAGE_NAME@.provider.storage" }, Exported = false, GrantUriPermissions = true)]
	[MetaData("android.support.FILE_PROVIDER_PATHS", Resource = "@xml/provider_paths")]
	class GenericFileProvider : Android.Support.V4.Content.FileProvider
	{
	}
}