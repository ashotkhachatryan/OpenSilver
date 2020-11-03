using System;
using System.Collections;
using System.Diagnostics;

#if MIGRATION
using System.Windows.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif

namespace OpenSilver.Internal.Controls
{
    /// <summary>
    /// Returns an Enumerator that enumerates over nothing.
    /// </summary>
    internal class EmptyEnumerator : IEnumerator
    {
        // singleton class, private ctor
        private EmptyEnumerator()
        {
        }

        /// <summary>
        /// Read-Only instance of an Empty Enumerator.
        /// </summary>
        public static IEnumerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EmptyEnumerator();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Reset() { }

        /// <summary>
        /// Returns false.
        /// </summary>
        /// <returns>false</returns>
        public bool MoveNext() { return false; }

        /// <summary>
        /// Returns null.
        /// </summary>
        public object Current
        {
            get
            {
                throw new InvalidOperationException();
            }
        }
        
        private static IEnumerator _instance;
    }

    internal class SingleChildEnumerator : IEnumerator
    {
        internal SingleChildEnumerator(object Child)
        {
            _child = Child;
            _count = Child == null ? 0 : 1;
        }

        object IEnumerator.Current
        {
            get { return (_index == 0) ? _child : null; }
        }

        bool IEnumerator.MoveNext()
        {
            _index++;
            return _index < _count;
        }

        void IEnumerator.Reset()
        {
            _index = -1;
        }

        private int _index = -1;
        private int _count = 0;
        private object _child;
    }

    internal abstract class ModelTreeEnumerator : IEnumerator
    {
        internal ModelTreeEnumerator(object content)
        {
            _content = content;
        }

#region IEnumerator

        object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        bool IEnumerator.MoveNext()
        {
            return this.MoveNext();
        }

        void IEnumerator.Reset()
        {
            this.Reset();
        }

#endregion

#region Protected

        protected object Content
        {
            get
            {
                return _content;
            }
        }

        protected int Index
        {
            get
            {
                return _index;
            }

            set
            {
                _index = value;
            }
        }

        protected virtual object Current
        {
            get
            {
                // Don't VerifyUnchanged(); According to MSDN:
                //     If the collection is modified between MoveNext and Current,
                //     Current will return the element that it is set to, even if
                //     the enumerator is already invalidated.

                if (_index == 0)
                {
                    return _content;
                }

                // Fall through -- can't enumerate (before beginning or after end)
                throw new InvalidOperationException("Enumerator is located before the first element of the collection or after the last element.");
                // above exception is part of the IEnumerator.Current contract when moving beyond begin/end
            }
        }

        protected virtual bool MoveNext()
        {
            if (_index < 1)
            {
                // Singular content, can move next to 0 and that's it.
                _index++;

                if (_index == 0)
                {
                    // don't call VerifyUnchanged if we're returning false anyway.
                    // This permits users to change the Content after enumerating
                    // the content (e.g. in the invalidation callback of an inherited
                    // property).  See bug 955389.

                    VerifyUnchanged();
                    return true;
                }
            }

            return false;
        }

        protected virtual void Reset()
        {
            VerifyUnchanged();
            _index = -1;
        }

        protected abstract bool IsUnchanged
        {
            get;
        }

        protected void VerifyUnchanged()
        {
            // If the content has changed, then throw an exception
            if (!IsUnchanged)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }
        }

#endregion

#region Data

        private int _index = -1;
        private object _content;

#endregion
    }

    internal class ContentModelTreeEnumerator : ModelTreeEnumerator
    {
        internal ContentModelTreeEnumerator(ContentControl contentControl, object content) : base(content)
        {
            Debug.Assert(contentControl != null, "contentControl should be non-null.");

            _owner = contentControl;
        }

        protected override bool IsUnchanged
        {
            get
            {
                return Object.ReferenceEquals(Content, _owner.Content);
            }
        }

        private ContentControl _owner;
    }

    /* HeaderedContentModelTreeEnumerator
    // Unused for now as it does not reflect the Silverlight logic of a HeaderContentControl.
    internal class HeaderedContentModelTreeEnumerator : ModelTreeEnumerator
    {
        internal HeaderedContentModelTreeEnumerator(HeaderedContentControl headeredContentControl, object content, object header) : base(header)
        {
            Debug.Assert(headeredContentControl != null, "headeredContentControl should be non-null.");
            Debug.Assert(header != null, "Header should be non-null. If Header was null, the base ContentControl enumerator should have been used.");

            _owner = headeredContentControl;
            _content = content;
        }

        protected override object Current
        {
            get
            {
                if ((Index == 1) && (_content != null))
                {
                    return _content;
                }

                return base.Current;
            }
        }

        protected override bool MoveNext()
        {
            if (_content != null)
            {
                if (Index == 0)
                {
                    // Moving from the header to content
                    Index++;
                    VerifyUnchanged();
                    return true;
                }
                else if (Index == 1)
                {
                    // Going from content to the end
                    Index++;
                    return false;
                }
            }

            return base.MoveNext();
        }

        protected override bool IsUnchanged
        {
            get
            {
                object header = Content;    // Header was passed to the base so that it would appear in index 0
                return Object.ReferenceEquals(header, _owner.Header) &&
                       Object.ReferenceEquals(_content, _owner.Content);
            }
        }

        private HeaderedContentControl _owner;
        private object _content;
    }
    */
}
