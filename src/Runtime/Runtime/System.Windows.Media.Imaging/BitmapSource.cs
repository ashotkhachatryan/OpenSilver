
/*===================================================================================
* 
*   Copyright (c) Userware/OpenSilver.net
*      
*   This file is part of the OpenSilver Runtime (https://opensilver.net), which is
*   licensed under the MIT license: https://opensource.org/licenses/MIT
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/

using DotNetForHtml5;
using DotNetForHtml5.Core;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#if MIGRATION
namespace System.Windows.Media.Imaging
#else
namespace Windows.UI.Xaml.Media.Imaging
#endif
{
    /// <summary>
    /// Provides a source object for properties that use a bitmap.
    /// </summary>
    public abstract partial class BitmapSource : ImageSource
    {
        internal object BitmapCanvas { get; set; }

        bool _isStreamAsBase64StringValid = false;

        private string _streamAsBase64String;
        public string INTERNAL_StreamAsBase64String
        {
            get
            {
                if (!_isStreamAsBase64StringValid)
                {
                    byte[] bytes = new byte[INTERNAL_StreamSource.Length];//note: if s.Length is longer than int.MaxValue, that's a problem... But that means they have a stream of more than 2 Go...
                    if (INTERNAL_StreamSource.Length > int.MaxValue)
                    {
                        throw new InvalidOperationException("The Stream set as the BitmapSource's Source is too big (more than int.MaxValue (2,147,483,647) bytes).");
                    }

                    int n = INTERNAL_StreamSource.Read(bytes, 0, (int)INTERNAL_StreamSource.Length);

                    //the following (commented) is in case the previous line doesn't work.
                    //int numBytesToRead = (int)s.Length; 
                    //int numBytesRead = 0;
                    //do
                    //{
                    //    // Read may return anything from 0 to 10.
                    //    int n = s.Read(bytes, numBytesRead, 10);
                    //    numBytesRead += n;
                    //    numBytesToRead -= n;
                    //} while (numBytesToRead > 0);
                    INTERNAL_StreamSource.Close();
                    _streamAsBase64String = Convert.ToBase64String(bytes);
                    _isStreamAsBase64StringValid = true;
                }
                return _streamAsBase64String;
            }
        }

        /// <summary>
        /// Provides base class initialization behavior for BitmapSource-derived classes.
        /// </summary>
        protected BitmapSource() :
            base()
        {
            OpenSilver.Interop.ExecuteJavaScript(@"
document.pixelData = '';
");
        }

        private Stream _streamSource;
        public Stream INTERNAL_StreamSource
        {
            get { return _streamSource; }
            private set { _streamSource = value; }
        }


        internal string _dataUrl;
        public string INTERNAL_DataURL
        {
            get { return _dataUrl; }
            private set { _dataUrl = value; }
        }

        /// <summary>
        /// Sets the source image for a BitmapSource by accessing a stream.
        /// </summary>
        /// <param name="streamSource">The stream source that sets the image source value.</param>
        public void SetSource(Stream streamSource) //note: this is supposed to be a IRandomAccessStream
        {
            // Copying the original stream because it could be disposed by the user before it is consumed
            // by the target image
            MemoryStream streamCopy = new MemoryStream();
            streamSource.CopyTo(streamCopy);
            streamCopy.Seek(0, SeekOrigin.Begin);

            INTERNAL_StreamSource = streamCopy;
            _isStreamAsBase64StringValid = false; //in case we set the source after having already set it and used it.
        }


        /// <summary>
        /// Sets the source image for a BitmapSource by passing a "data URL".
        /// </summary>
        /// <param name="dataUrl">The image encoded in "data URL" format.</param>
        public void SetSource(string dataUrl)
        {
            INTERNAL_DataURL = dataUrl;
        }

        public Task<bool> SetSourceAsync(Stream stream)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Action doneCallback = () =>
            {
                tcs.SetResult(true);
            };
            SetSource(stream, doneCallback);
            return tcs.Task;
        }

        public void SetSource(Stream stream, Action doneCallback)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);

            string base64 = Convert.ToBase64String(bytes);
            string url = "data:image/png;base64," + base64;
            LoadImage(url, () =>
            {
                doneCallback();
            });
        }

        public void LoadImage(Uri uri, Action doneCallback)
        {
            string url = uri.OriginalString;
            if (!uri.IsAbsoluteUri)
            {
                string callerAssemblyName = Assembly.GetCallingAssembly().GetName().Name;
                url = "/" + callerAssemblyName + ";component/" + uri.OriginalString;
            }
            LoadImage(url, doneCallback);
        }

        //internal string _dataURL = "";
        internal int _pixelWidth = 0;
        internal int _pixelHeight = 0;
        internal int[] _pixels = new int[0];
        public void LoadImage(string url, Action doneCallback)
        {
            Action<string, string, string> callback = (width, height, dataURL) =>
            {
                this._dataUrl = dataURL;
                INTERNAL_DataURL = this._dataUrl;
                this._pixelWidth = int.Parse(width);
                this._pixelHeight = int.Parse(height);


                int arraySize = this._pixelWidth * this._pixelHeight * 4;
                byte[] bytes = new byte[arraySize];
                //IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
                INTERNAL_Simulator.JavaScriptExecutionHandler.InvokeUnmarshalled<byte[], object>("document.getData", bytes);

                // byte[0] - R
                // byte[1] - G
                // byte[2] - B
                // byte[3] - A (alpha)

                _pixels = new int[bytes.Length / 4];
                for (int i = 0; i < bytes.Length; i += 4)
                {
                    byte[] b = null;
                    //if (BitConverter.IsLittleEndian)
                    //{
                        b = new byte[4] { bytes[i + 2], bytes[i + 1], bytes[i], bytes[i + 3] };
                    //}
                    //else
                    //{
                    //    b = new byte[4] { bytes[i + 3], bytes[i], bytes[i + 1], bytes[i + 2] };
                    //}
                    _pixels[(i + 1) / 4] = BitConverter.ToInt32(b, 0);
                }

                OnSourceChanged();
                doneCallback();
            };

            OpenSilver.Interop.ExecuteJavaScript(@"
document.pixelData = '';
var img = new Image();
img.src = $1;
img.onload = function() {
    let canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    canvas.height = img.height;
    canvas.width = img.width;
    ctx.drawImage(img, 0, 0);
    const dataURL = canvas.toDataURL();
    var pixelData = canvas.getContext('2d').getImageData(0, 0, img.width, img.height).data;
    document.pixelData = pixelData;
    canvas = null;

    callback = $2;
    callback(img.width, img.height, dataURL);
};

document.getData = function(bufferPointer) {
    const dataPtr = Blazor.platform.getArrayEntryPtr(bufferPointer, 0, 4);
    const length = Blazor.platform.getArrayLength(bufferPointer);
    var shorts = new Uint8Array(Module.HEAPU8.buffer, dataPtr, length);
    shorts.set(new Uint8Array(document.pixelData), 0);
}
", BitmapCanvas, url, callback);
        }

        internal virtual void OnSourceChanged()
        {

        }

        #region Not supported yet
        /// <summary>
        /// Gets the height of the bitmap in pixels.
        /// </summary>
        public int PixelHeight
        {
            get => _pixelHeight;
        }

        /// <summary>
        /// Identifies the PixelHeight dependency property.
        /// 
        /// Returns the identifier for the PixelHeight dependency property.
        /// </summary>
        //[OpenSilver.NotImplemented]
        //public static readonly DependencyProperty PixelHeightProperty = DependencyProperty.Register("PixelHeight", typeof(int), typeof(BitmapSource), new PropertyMetadata(0));

        /// <summary>
        /// Gets the width of the bitmap in pixels.
        /// </summary>
        //[OpenSilver.NotImplemented]
        public int PixelWidth
        {
            get => _pixelWidth;
        }

        /// <summary>
        /// Identifies the PixelWidth dependency property.
        /// 
        /// Returns the identifier for the PixelWidth dependency property.
        /// </summary>
        //[OpenSilver.NotImplemented]
        //public static readonly DependencyProperty PixelWidthProperty = DependencyProperty.Register("PixelWidth", typeof(int), typeof(BitmapSource), new PropertyMetadata(0));


        ////
        //// Summary:
        ////     Sets the source image for a BitmapSource by accessing a stream and processing
        ////     the result asynchronously.
        ////
        //// Parameters:
        ////   streamSource:
        ////     The stream source that sets the image source value.
        ////
        //// Returns:
        ////     An asynchronous handler called when the operation is complete.
        //public IAsyncAction SetSourceAsync(IRandomAccessStream streamSource);
        #endregion
    }
}
