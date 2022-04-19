using System.Windows.Media;
using System.Windows;
using System;
using System.Threading.Tasks;

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
            Action<string, string, string, string> callback = (width, height, dataURL, pixelData) =>
            {
                this._dataUrl = dataURL;
                this._pixelWidth = int.Parse(width);
                this._pixelHeight = int.Parse(height);

                byte[] bytes = Convert.FromBase64String(pixelData);
                if (bytes.Length / 4 != 0)
                {
                    // We have an issue here
                }
                // byte[0] - R
                // byte[1] - G
                // byte[2] - B
                // byte[3] - A (alpha)

                _pixels = new int[bytes.Length / 4];
                for (int i = 0; i < bytes.Length; i += 4)
                {
                    byte[] b = new byte[4] { bytes[i + 3], bytes[i], bytes[i + 1], bytes[i + 2] };
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    _pixels[(i + 1) / 4] = BitConverter.ToInt32(b, 0);
                }

                Console.WriteLine("CALLING OnSourceChanged");
                OnSourceChanged();

                //if (Loaded != null)
                //	Loaded(this, new EventArgs());
                doneCallback?.Invoke();
            };

            OpenSilver.Interop.ExecuteJavaScript(@"

function arrayBufferToBase64( buffer ) {
    var binary = '';
    var bytes = new Uint8Array( buffer );
    var len = bytes.byteLength;
    for (var i = 0; i < len; i++) {
        binary += String.fromCharCode( bytes[ i ] );
    }
    return window.btoa( binary );
}

var script = document.createElement('script');
script.setAttribute('type', 'application/javascript');
script.setAttribute('src', 'libs/html2canvas.js');
document.getElementsByTagName('head')[0].appendChild(script);

script.onload = function() {
    //var el = document.getElementById('id9');
    var el = $0;
    html2canvas(el).then(canvas => {
        document.body.appendChild(canvas);

        const dataURL = canvas.toDataURL();
        var pixelData = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data;

        callback = $1
        callback(canvas.width, canvas.height, dataURL, arrayBufferToBase64(pixelData));
    });

    script.remove();
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
            WriteableBitmap bitmap = null;
            bitmap = new WriteableBitmap(element, transform, () =>
            {
                Console.WriteLine("&&&&& PIXELS LENGTH " + this.Pixels.Length + " " + bitmap.Pixels.Length + " " + bitmap.PixelWidth + " " + bitmap.PixelHeight);

                for (int i = 0; i < bitmap.PixelWidth * bitmap.PixelHeight; i++)
                {
                    int i1 = i / bitmap.PixelWidth;
                    int j1 = i % bitmap.PixelWidth;

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
            Action<string, string, string, string> callback = (width, height, dataURL, pixelData) =>
            {
                this._dataUrl = dataURL;
                this._pixelWidth = int.Parse(width);
                this._pixelHeight = int.Parse(height);

                byte[] bytes1 = Convert.FromBase64String(pixelData);
                if (bytes1.Length / 4 != 0)
                {
                    // We have an issue here
                }
                // byte[0] - R
                // byte[1] - G
                // byte[2] - B
                // byte[3] - A (alpha)

                _pixels = new int[bytes1.Length / 4];
                for (int i = 0; i < bytes1.Length; i += 4)
                {
                    byte[] b = new byte[4] { bytes1[i + 3], bytes1[i], bytes1[i + 1], bytes1[i + 2] };
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    _pixels[(i + 1) / 4] = BitConverter.ToInt32(b, 0);
                }
                OnSourceChanged();
                doneCallback?.Invoke();
            };

            byte[] bytes = new byte[this._pixels.Length * 4];
            for (int i = 0; i < this._pixels.Length; i++)
            {
                byte[] b = BitConverter.GetBytes(this._pixels[i]);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }

                byte temp = b[0];
                b[0] = b[1];
                b[1] = b[2];
                b[2] = b[3];
                b[3] = temp;

                bytes[i * 4] = b[0];
                bytes[i * 4 + 1] = b[1];
                bytes[i * 4 + 2] = b[2];
                bytes[i * 4 + 3] = b[3];
            }

            string base64 = Convert.ToBase64String(bytes);
            OpenSilver.Interop.ExecuteJavaScript(@"
function convertDataURIToBinary(dataURI) {
  var raw = window.atob(dataURI);
  var rawLength = raw.length;
  var array = new Uint8Array(new ArrayBuffer(rawLength));

  for(i = 0; i < rawLength; i++) {
    array[i] = raw.charCodeAt(i);
  }
  return array;
}

function arrayBufferToBase641( buffer ) {
    var binary = '';
    var bytes = new Uint8Array( buffer );
    var len = bytes.byteLength;
    for (var i = 0; i < len; i++) {
        binary += String.fromCharCode( bytes[ i ] );
    }
    return window.btoa( binary );
}

var bytes = convertDataURIToBinary($0);

var canvas = document.createElement('canvas');
canvas.width = $1;
canvas.height = $2;
canvas.getContext('2d').createImageData(canvas.width, canvas.height);
var pixelData = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height);

for (var i = 0; i < bytes.length; i++) {
pixelData.data[i] = bytes[i];
}

canvas.getContext('2d').putImageData(pixelData, 0, 0);

var callback = $3;

callback(canvas.width, canvas.height, canvas.toDataURL(), arrayBufferToBase641(pixelData.data));
", base64, PixelWidth, PixelHeight, callback);
            //}
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
