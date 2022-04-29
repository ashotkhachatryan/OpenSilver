using System.Windows.Media;
using System.Windows;
using System;
using System.Threading.Tasks;
using DotNetForHtml5.Core;

#if MIGRATION
namespace System.Windows.Media.Imaging
#else
namespace Windows.UI.Xaml.Media.Imaging
#endif
{
    public sealed partial class WriteableBitmap : BitmapSource
    {
        public int[] Pixels
        {
            get
            {
                return _pixels;
            }
        }

        public WriteableBitmap(BitmapSource source)
        {
            // Initialize from source
            _pixelWidth = source.PixelWidth;
            _pixelHeight = source.PixelHeight;

            _pixels = new int[source._pixels.Length];
            Array.Copy(source._pixels, _pixels, source._pixels.Length);

            _dataUrl = source._dataUrl;
        }

        public WriteableBitmap(int pixelWidth, int pixelHeight)
        {
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
            _pixels = new int[pixelWidth * pixelHeight];
        }

        public static Task<WriteableBitmap> CreateFromUIElement(UIElement element, Transform transform)
        {
            TaskCompletionSource<WriteableBitmap> tcs = new TaskCompletionSource<WriteableBitmap>();
            WriteableBitmap bitmap = null;

            Action doneCallback = () =>
            {
                tcs.SetResult(bitmap);
            };

            bitmap = new WriteableBitmap(element, transform, doneCallback);

            return tcs.Task;
        }

        public WriteableBitmap(UIElement element, Transform transform, Action doneCallback = null)
        {
            Action<string, string, string> callback = (width, height, dataURL) =>
            {
                this._dataUrl = dataURL;
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
                doneCallback?.Invoke();
            };

            OpenSilver.Interop.ExecuteJavaScript(@"

var script = document.createElement('script');
script.setAttribute('type', 'application/javascript');
script.setAttribute('src', 'libs/html2canvas.js');
document.getElementsByTagName('head')[0].appendChild(script);

document.pixelData = '';

script.onload = function() {
    var el = $0;
    html2canvas(el).then(canvas => {
        document.body.appendChild(canvas);

        const dataURL = canvas.toDataURL();
        document.pixelData = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data;

        callback = $1
        callback(canvas.width, canvas.height, dataURL);
    });

    script.remove();
}

document.getData = function(bufferPointer) {
    const dataPtr = Blazor.platform.getArrayEntryPtr(bufferPointer, 0, 4);
    const length = Blazor.platform.getArrayLength(bufferPointer);
    var shorts = new Uint8Array(Module.HEAPU8.buffer, dataPtr, length);
    shorts.set(new Uint8Array(document.pixelData), 0);
}

", element.INTERNAL_InnerDomElement, callback);
        }

        public Task<bool> Render(UIElement element, Transform transform)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Action doneCallback = () =>
            {
                tcs.SetResult(true);
            };

            Render(element, transform, doneCallback);

            return tcs.Task;
        }

        public void Render(UIElement element, Transform transform, Action doneCallback = null)
        {
            int X = 0;
            int Y = 0;
            if (transform is TranslateTransform tt)
            {
                X = (int)tt.X;
                Y = (int)tt.Y;
            }
            WriteableBitmap bitmap = null;
            bitmap = new WriteableBitmap(element, transform, () =>
            {
                for (int i = 0; i < bitmap.PixelWidth * bitmap.PixelHeight; i++)
                {
                    int i1 = Y + i / bitmap.PixelWidth;
                    int j1 = X + i % bitmap.PixelWidth;

                    if (i1 * this.PixelWidth + j1 >= this.PixelWidth * this.PixelHeight)
                        continue;

                    this.Pixels[i1 * this.PixelWidth + j1] = bitmap.Pixels[i];
                }

                doneCallback?.Invoke();
            });
        }

        public Task<bool> Invalidate()
        {
            TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();

            Action doneCallback = () =>
            {
                tsc.SetResult(true);
            };

            Invalidate(doneCallback);

            return tsc.Task;
        }

        public void Invalidate(Action doneCallback = null)
        {
            //if (this._dataUrl != null)
            //{
            Action<string, string, string> callback = (width, height, dataURL) =>
            {
                this._dataUrl = dataURL;
                this._pixelWidth = int.Parse(width);
                this._pixelHeight = int.Parse(height);

                int arraySize = this._pixelWidth * this._pixelHeight * 4;
                byte[] bytes1 = new byte[arraySize];

                INTERNAL_Simulator.JavaScriptExecutionHandler.InvokeUnmarshalled<byte[], object>("document.getData", bytes1);
                // byte[0] - R
                // byte[1] - G
                // byte[2] - B
                // byte[3] - A (alpha)

                _pixels = new int[bytes1.Length / 4];
                for (int i = 0; i < bytes1.Length; i += 4)
                {
                    byte[] b = null;
                    //if (BitConverter.IsLittleEndian)
                    //{
                        b = new byte[4] { bytes1[i + 2], bytes1[i + 1], bytes1[i], bytes1[i + 3] };
                    //}
                    //else
                    //{
                    //    b = new byte[4] { bytes1[i + 3], bytes1[i], bytes1[i + 1], bytes1[i + 2] };
                    //}
                    _pixels[(i + 1) / 4] = BitConverter.ToInt32(b, 0);
                }
                OnSourceChanged();
                doneCallback?.Invoke();
            };

            byte[] bytes = new byte[this._pixels.Length * 4];
            for (int i = 0; i < this._pixels.Length; i++)
            {
                byte[] b = BitConverter.GetBytes(this._pixels[i]);

                //if (BitConverter.IsLittleEndian)
                //{
                    bytes[i * 4] = b[2];
                    bytes[i * 4 + 1] = b[1];
                    bytes[i * 4 + 2] = b[0];
                    bytes[i * 4 + 3] = b[3];
                //}
                //else
                //{
                //    bytes[i * 4] = b[1];
                //    bytes[i * 4 + 1] = b[2];
                //    bytes[i * 4 + 2] = b[3];
                //    bytes[i * 4 + 3] = b[0];
                //}
            }

            OpenSilver.Interop.ExecuteJavaScript(@"
document.bytes = '';
document.receiveBytes = function(bufferPointer) {
    const dataPtr = Blazor.platform.getArrayEntryPtr(bufferPointer, 0, 4);
    const length = Blazor.platform.getArrayLength(bufferPointer);
    document.bytes = new Uint8Array(Module.HEAPU8.buffer, dataPtr, length);
};
");

            INTERNAL_Simulator.JavaScriptExecutionHandler.InvokeUnmarshalled<byte[], object>("document.receiveBytes", bytes);

            OpenSilver.Interop.ExecuteJavaScript(@"
var canvas = document.createElement('canvas');
canvas.width = $0;
canvas.height = $1;
canvas.getContext('2d').createImageData(canvas.width, canvas.height);
var pixelData = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height);

pixelData.data.set(new Uint8ClampedArray(document.bytes));
canvas.getContext('2d').putImageData(pixelData, 0, 0);

var callback = $2;

document.pixelData = pixelData.data;
callback(canvas.width, canvas.height, canvas.toDataURL());
", PixelWidth, PixelHeight, callback);
        }

        public event EventHandler Loaded;

        public event EventHandler UriSourceChanged;
        protected void OnUriSourceChanged()
        {
            if (UriSourceChanged != null)
            {
                UriSourceChanged(this, new EventArgs());
            }
        }

        internal override void OnSourceChanged()
        {
            OnUriSourceChanged();
        }
    }
}
