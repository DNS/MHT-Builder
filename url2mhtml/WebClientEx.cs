using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace MHTMLBuilder
{
    /// <summary>
    /// This is a class similar to <c>System.Net.WebClient</c>, but with:
    ///   - Autodetection of content encoding
    ///   - Proxy support
    ///   - Authentication support
    ///   - Cookie retention
    ///   - HTTP compression (via SharpZipLib)
    ///   - IfModifiedSince
    /// </summary>
    /// <remarks>
    ///   Jeff Atwood
    ///   http://www.codinghorror.com/
    /// </remarks>
    internal class WebClientEx
    {
        private const string _AcceptedEncodings = "gzip,deflate";
        private string _AuthenticationPassword;
        private bool _AuthenticationRequired;
        private string _AuthenticationUser;
        private string _ContentLocation;
        private System.Text.Encoding _DefaultEncoding;
        private string _DetectedContentType;
        private System.Text.Encoding _DetectedEncoding;
        private System.Text.Encoding _ForcedEncoding;
        private string _HttpUserAgent;

        private bool _KeepCookies;
        private CookieContainer _PersistedCookies;
        private bool _ProxyAuthenticationRequired;
        private string _ProxyPassword;
        private string _ProxyUrl;
        private string _ProxyUser;

        private int _RequestTimeoutMilliseconds;
        private byte[] _ResponseBytes;

        public WebClientEx()
        {
            //-- sets default values
            Clear();
        }

        #region "Property"

        /// <summary>
        /// this is the string encoding that was autodetected from the HTML content
        /// </summary>
        public System.Text.Encoding DetectedEncoding
        {
            get { return _DetectedEncoding; }
        }


        /// <summary>
        /// Bypass detection of content encoding and force a specific encoding
        /// </summary>
        public System.Text.Encoding ForcedEncoding
        {
            get { return _ForcedEncoding; }
            set { _ForcedEncoding = value; }
        }

        /// <summary>
        /// if the correct string encoding type cannot be detected, or detection is disabled
        /// this is the default string encoding that will be used.
        /// </summary>
        public System.Text.Encoding DefaultEncoding
        {
            get { return _DefaultEncoding; }
            set { _DefaultEncoding = value; }
        }

        /// <summary>
        /// this is the string encoding that was autodetected from the HTML content
        /// </summary>
        public string ResponseContentType
        {
            get { return _DetectedContentType; }
        }

        /// <summary>
        /// Returns true if the last HTTP response was in a non-text format
        /// </summary>
        public bool ResponseIsBinary
        {
            get
            {
                //-- if we truly have no content-type, we're kinda hosed, but 
                //-- let's assume the response is text data to be on the safe side
                if (string.IsNullOrEmpty(_DetectedContentType))
                {
                    return false;
                }
                else
                {
                    return _DetectedContentType.IndexOf("text") == -1;
                }
            }
        }


        /// <summary>
        /// Returns a string representation, with encoding, of the last HTTP response data
        /// </summary>
        public string ResponseString
        {
            get
            {
                if (ResponseIsBinary)
                {
                    return "(" + _ResponseBytes.Length + " bytes of binary data)";
                }
                else
                {
                    if (_ForcedEncoding == null)
                    {
                        return _DetectedEncoding.GetString(_ResponseBytes);
                    }
                    else
                    {
                        return _ForcedEncoding.GetString(_ResponseBytes);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the raw bytestream representing the last HTTP response data
        /// </summary>
        public byte[] ResponseBytes
        {
            get { return _ResponseBytes; }
        }

        /// <summary>
        /// Returns the actual location of the downloaded content, which can 
        /// be different than the requested URL, eg, http://web.com/IsThisAfolderOrNot
        /// </summary>
        public string ContentLocation
        {
            get { return _ContentLocation; }
        }


        /// <summary>
        /// Browser ID string to send with web requests
        /// note that many popular websites will serve alternate content based on this value
        /// </summary>
        /// <remarks>
        /// defaults to browser ID string of vanilla IE6 as seen in XP SP2
        /// </remarks>
        public string BrowserIdString
        {
            get { return _HttpUserAgent; }
            set { _HttpUserAgent = value; }
        }

        /// <summary>
        /// how long, in milliseconds, to wait for HTTP content to arrive before timing out
        /// </summary>
        public int TimeoutMilliseconds
        {
            get { return _RequestTimeoutMilliseconds; }
            set { _RequestTimeoutMilliseconds = value; }
        }

        /// <summary>
        /// URL of the web proxy to use
        /// if left blank, no Proxy will be used; if provided, will ALWAYS be used!
        /// </summary>
        public string ProxyUrl
        {
            get { return _ProxyUrl; }
            set { _ProxyUrl = value; }
        }

        /// <summary>
        /// username to use for Proxy authentication
        /// if left blank, the current user's credentials will be sent
        /// </summary>
        public string ProxyUser
        {
            get { return _ProxyUser; }
            set { _ProxyUser = value; }
        }


        /// <summary>
        /// password to use for Proxy authentication
        /// if left blank, the current user's credentials will be sent
        /// </summary>
        public string ProxyPassword
        {
            get { return _ProxyPassword; }
            set { _ProxyPassword = value; }
        }


        /// <summary>
        /// password for authentication to the target URL
        /// if left blank, the current user's credentials will be sent
        /// </summary>
        public string AuthenticationPassword
        {
            get { return _AuthenticationPassword; }
            set { _AuthenticationPassword = value; }
        }

        /// <summary>
        /// the Proxy requires authentication
        /// if credentials are not explicitly provided, the current user's credentials will automatically be sent
        /// </summary>
        public bool ProxyAuthenticationRequired
        {
            get { return _ProxyAuthenticationRequired; }
            set { _ProxyAuthenticationRequired = value; }
        }


        /// <summary>
        /// the target URL requires authentication
        /// if credentials are not explicitly provided, the current user's credentials will automatically be sent
        /// </summary>
        public bool AuthenticationRequired
        {
            get { return _AuthenticationRequired; }
            set { _AuthenticationRequired = value; }
        }

        /// <summary>
        /// username for authentication to the target URL
        /// if left blank, the current user's credentials will be sent
        /// </summary>
        public string AuthenticationUser
        {
            get { return _AuthenticationUser; }
            set { _AuthenticationUser = value; }
        }

        /// <summary>
        /// Retains cookies for all subsequent HTTP requests from this object
        /// </summary>
        public bool KeepCookies
        {
            get { return _KeepCookies; }
            set { _KeepCookies = value; }
        }

        #endregion "Property"

        /// <summary>
        /// attempt to convert this charset string into a named .NET text encoding
        /// </summary>
        private System.Text.Encoding CharsetToEncoding(string Charset)
        {
            if (string.IsNullOrEmpty(Charset)) return null;

            try
            {
                return System.Text.Encoding.GetEncoding(Charset);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// try to determine string encoding using Content-Type HTTP header and
        /// raw HTTP content bytes
        /// "Content-Type: text/html; charset=us-ascii"
        /// &lt;meta http-equiv="Content-Type" content="text/html; charset=utf-8"/&gt;
        /// </summary>
        private System.Text.Encoding DetectEncoding(string ContentTypeHeader, byte[] ResponseBytes)
        {
            string s = null;

            System.Text.Encoding encoding = default(System.Text.Encoding);

            //-- first try the header
            s =
                Regex.Match(ContentTypeHeader, "charset=([^;\"'/>]+)", RegexOptions.IgnoreCase).Groups[1].ToString().
                    ToLower();

            encoding = CharsetToEncoding(s);

            //-- if we can't get it from header, try the body bytes forced to ASCII
            if (encoding == null)
            {
                s =
                    Regex.Match(System.Text.Encoding.ASCII.GetString(ResponseBytes),
                                "<meta[^>]+content-type[^>]+charset=([^;\"'/>]+)", RegexOptions.IgnoreCase).Groups[1].
                        ToString().ToLower();

                encoding = CharsetToEncoding(s);

                if (encoding == null)
                {
                    return _DefaultEncoding;
                }
            }

            return encoding;
        }

        /// <summary>
        /// returns a collection of bytes from a Url
        /// </summary>
        /// <param name="Url">URL to retrieve</param>
        public void GetUrlData(string Url, DateTime ifModifiedSince)
        {
            if (!string.IsNullOrEmpty(Url))
            {
                HttpWebRequest wreq = (HttpWebRequest)WebRequest.Create(Url);

                //-- do we need to use a proxy to get to the web?
                if (!string.IsNullOrEmpty(_ProxyUrl))
                {
                    WebProxy wp = new WebProxy(_ProxyUrl);

                    if (_ProxyAuthenticationRequired)
                    {
                        if (!string.IsNullOrEmpty(_ProxyUser) & !string.IsNullOrEmpty(_ProxyPassword))
                        {
                            wp.Credentials = new NetworkCredential(_ProxyUser, _ProxyPassword);
                        }
                        else
                        {
                            wp.Credentials = CredentialCache.DefaultCredentials;
                        }
                        wreq.Proxy = wp;
                    }
                }

                //-- does the target website require credentials?
                if (_AuthenticationRequired)
                {
                    if (!string.IsNullOrEmpty(_AuthenticationUser) & !string.IsNullOrEmpty(_AuthenticationPassword))
                    {
                        wreq.Credentials = new NetworkCredential(_AuthenticationUser, _AuthenticationPassword);
                    }
                    else
                    {
                        wreq.Credentials = CredentialCache.DefaultCredentials;
                    }
                }

                wreq.Method = "GET";
                wreq.Timeout = _RequestTimeoutMilliseconds;
                wreq.UserAgent = _HttpUserAgent;
                wreq.Headers.Add("Accept-Encoding", _AcceptedEncodings);

                //-- note that, if present, this will trigger a 304 exception
                //-- if the URL being retrieved is not newer than the specified
                //-- date/time
                if (ifModifiedSince != DateTime.MinValue)
                {
                    wreq.IfModifiedSince = ifModifiedSince;
                }

                //-- sometimes we need to transfer cookies to another URL; 
                //-- this keeps them around in the object
                if (KeepCookies)
                {
                    if (_PersistedCookies == null)
                    {
                        _PersistedCookies = new CookieContainer();
                    }
                    wreq.CookieContainer = _PersistedCookies;
                }

                //-- download the target URL into a byte array
                HttpWebResponse wresp = (HttpWebResponse)wreq.GetResponse();

                //-- convert response stream to byte array
                ExtendedBinaryReader ebr = new ExtendedBinaryReader(wresp.GetResponseStream());
                _ResponseBytes = ebr.ReadToEnd();

                //-- determine if body bytes are compressed, and if so, 
                //-- decompress the bytes
                HttpContentEncoding ContentEncoding = default(HttpContentEncoding);

                if (wresp.Headers["Content-Encoding"] == null)
                {
                    ContentEncoding = HttpContentEncoding.None;
                }
                else
                {
                    switch (wresp.Headers["Content-Encoding"].ToLower())
                    {
                        case "gzip":
                            ContentEncoding = HttpContentEncoding.Gzip;
                            break;
                        case "deflate":
                            ContentEncoding = HttpContentEncoding.Deflate;
                            break;
                        default:
                            ContentEncoding = HttpContentEncoding.Unknown;
                            break;
                    }
                    _ResponseBytes = Decompress(_ResponseBytes, ContentEncoding);
                }

                //-- sometimes URL is indeterminate, eg, "http://website.com/myfolder"
                //-- in that case the folder and file resolution MUST be done on 
                //-- the server, and returned to the client as ContentLocation
                _ContentLocation = wresp.Headers["Content-Location"];

                if (_ContentLocation == null)
                {
                    _ContentLocation = "";
                }

                //-- if we have string content, determine encoding type
                //-- (must cast to prevent Nothing)
                _DetectedContentType = wresp.Headers["Content-Type"];

                if (_DetectedContentType == null)
                {
                    _DetectedContentType = "";
                }

                if (ResponseIsBinary)
                {
                    _DetectedEncoding = null;
                }
                else
                {
                    if (_ForcedEncoding == null)
                    {
                        _DetectedEncoding = DetectEncoding(_DetectedContentType, _ResponseBytes);
                    }
                }
            }
        }

        /// <summary>
        /// decompresses a compressed array of bytes via the specified HTTP compression type
        /// </summary>
        /// <returns>decompressed array of bytes</returns>
        private byte[] Decompress(byte[] b, HttpContentEncoding CompressionType)
        {
            Stream s = default(Stream);

            switch (CompressionType)
            {
                case HttpContentEncoding.Deflate:
                    s = new InflaterInputStream(new MemoryStream(b),
                                                new ICSharpCode.SharpZipLib.Zip.Compression.Inflater(true));
                    break;
                case HttpContentEncoding.Gzip:
                    s = new GZipInputStream(new MemoryStream(b));
                    break;
                default:
                    return b;
            }

            MemoryStream ms = new MemoryStream();

            int sizeRead = 0;
            const int chunkSize = 2048;


            byte[] unzipBytes = new byte[chunkSize + 1];

            while (true)
            {
                sizeRead = s.Read(unzipBytes, 0, chunkSize);
                if (sizeRead > 0)
                {
                    ms.Write(unzipBytes, 0, sizeRead);
                }
                else
                {
                    break; // TODO: might not be correct. Was : Exit While
                }
            }
            s.Close();

            return ms.ToArray();
        }

        /// <summary>
        /// download URL contents to a file, using HTTP compression if possible
        /// </summary>
        public void DownloadFile(string Url, string FilePath)
        {
            DownloadFile(Url, FilePath, DateTime.MinValue);
        }

        /// <summary>
        /// download URL contents to a file, using HTTP compression if possible
        /// URL contents will only be downloaded if newer than the specified date
        /// </summary>
        public void DownloadFile(string Url, string FilePath, DateTime ifModifiedSince)
        {
            GetUrlData(Url, ifModifiedSince);
            SaveResponseToFile(FilePath);
        }

        private void SaveResponseToFile(string FilePath)
        {
            //initialized to Nothing by Reza
            FileStream fs = null;

            BinaryWriter bw = default(BinaryWriter);

            try
            {
                fs = new FileStream(FilePath, FileMode.OpenOrCreate);
                bw = new BinaryWriter(fs);
                bw.Write(_ResponseBytes);
                bw.Close();
            }
            finally
            {
                if ((fs != null)) fs.Close();
            }
            return;
        }

        /// <summary>
        /// download URL contents to an array of bytes, using HTTP compression if possible
        /// </summary>
        public byte[] DownloadBytes(string Url)
        {
            return DownloadBytes(Url, DateTime.MinValue);
        }

        /// <summary>
        /// download URL contents to an array of bytes, using HTTP compression if possible
        /// URL contents will only be downloaded if newer than the specified date
        /// </summary>
        public byte[] DownloadBytes(string Url, DateTime ifModifiedSince)
        {
            GetUrlData(Url, ifModifiedSince);
            return _ResponseBytes;
        }

        /// <summary>
        /// download URL contents to a string (with appropriate encoding), using HTTP compression if possible
        /// </summary>
        public string DownloadString(string Url)
        {
            GetUrlData(Url, DateTime.MinValue);
            return ResponseString;
        }

        /// <summary>
        /// download URL contents to a string (with appropriate encoding), using HTTP compression if possible
        /// URL contents will only be downloaded if newer than the specified date
        /// </summary>
        public string DownloadString(string Url, DateTime ifModifiedSince)
        {
            GetUrlData(Url, ifModifiedSince);
            return ResponseString;
        }

        /// <summary>
        /// clears any downloaded content and resets all properties to default values
        /// </summary>
        public void Clear()
        {
            ClearDownload();
            _DefaultEncoding = System.Text.Encoding.GetEncoding("Windows-1252");
            _ForcedEncoding = null;
            _AuthenticationRequired = false;
            _ProxyAuthenticationRequired = false;
            _ProxyUrl = "";
            _ProxyUser = "";
            _ProxyPassword = "";
            _AuthenticationUser = "";
            _AuthenticationPassword = "";
            _KeepCookies = false;
            _RequestTimeoutMilliseconds = 60000;
            _HttpUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1)";
        }

        /// <summary>
        /// clears any downloaded content
        /// </summary>
        public void ClearDownload()
        {
            _ResponseBytes = null;
            _DetectedEncoding = null;
            _DetectedContentType = "";
            _ContentLocation = "";
            _PersistedCookies = null;
        }

        #region " ExtendedBinaryReader"

        /// <summary>
        ///   Extends the <c>System.IO.BinaryReader</c> class by a <c>ReadToEnd</c>
        ///   method that can be used to read a whole file.
        /// </summary>
        /// <remarks>
        /// http://dotnet.mvps.org/dotnet/faqs/?id=readfile&amp;lang=en
        /// </remarks>
        public class ExtendedBinaryReader : BinaryReader
        {
            /// <summary>
            ///   Creates a new instance of the <c>ExtendedBinaryReader</c> class.
            /// </summary>
            /// <param name="Input">A stream.</param>
            public ExtendedBinaryReader(Stream Input)
                : base(Input)
            {
            }

            /// <summary>
            ///   Creates a new instance of the <c>ExtendedBinaryReader</c> class.
            /// </summary>
            /// <param name="Input">The provided stream.</param>
            /// <param name="Encoding">The character encoding.</param>
            public ExtendedBinaryReader(Stream Input, System.Text.Encoding Encoding)
                : base(Input, Encoding)
            {
            }

            /// <summary>
            ///   Reads the whole data in the base stream and returns it in an
            ///   array of bytes.
            /// </summary>
            /// <returns>The streams whole binary data.</returns>
            public byte[] ReadToEnd()
            {
                return ReadToEnd(short.MaxValue);
            }


            // Based on an implementation by Jon Skeet [MVP]
            // (<URL:http://www.yoda.arachsys.com/csharp/readbinary.html>).
            /// <summary>
            ///   Reads the whole data in the base stream and returns it in an
            ///   array of bytes.
            /// </summary>
            /// <param name="InitialLength">The initial buffer length.</param>
            /// <returns>The stream's whole binary data.</returns>
            public byte[] ReadToEnd(int InitialLength)
            {
                // If an unhelpful initial length was passed, just use 32K.
                if (InitialLength < 1)
                {
                    InitialLength = short.MaxValue;
                }
                //*** added -1 by reza, removed the "InitialLength-1" for testing 
                byte[] Buffer = new byte[InitialLength];

                int Read = 0;
                int Chunk = BaseStream.Read(Buffer, Read, Buffer.Length - Read);
                while (Chunk > 0)
                {
                    Read = Read + Chunk;

                    // If the end of the buffer is reached, check to see if there is
                    // any more data.
                    if (Read == Buffer.Length)
                    {
                        int NextByte = BaseStream.ReadByte();

                        // If the end of the stream is reached, we are done.
                        if (NextByte == -1)
                        {
                            return Buffer;
                        }

                        // Nope.  Resize the buffer, put in the byte we have just
                        // read, and continue.

                        //added -1 by reza, removed the "-1" from "Buffer.Length * 2-1" for testing 
                        byte[] NewBuffer = new byte[Buffer.Length * 2];

                        System.Buffer.BlockCopy(Buffer, 0, NewBuffer, 0, Buffer.Length);
                        //Array.Copy(Buffer, NewBuffer, Buffer.Length)

                        NewBuffer[Read] = (byte)NextByte;
                        Buffer = NewBuffer;
                        Read = Read + 1;
                    }
                    Chunk = BaseStream.Read(Buffer, Read, Buffer.Length - Read);
                }

                // The buffer is now too big.  Shrink it.
                byte[] ReturnBuffer = new byte[Read];
                System.Buffer.BlockCopy(Buffer, 0, ReturnBuffer, 0, Read);

                //Array.Copy(Buffer, ReturnBuffer, Read)
                return ReturnBuffer;
            }
        }

        #endregion

        #region Nested type: HttpContentEncoding

        /// <summary>
        /// The Content-Encoding entity-header field is used as a modifier to the media-type. 
        /// When present, its value indicates what additional content codings have been applied 
        /// to the entity-body, and thus what decoding mechanisms must be applied in order to 
        /// obtain the media-type referenced by the Content-Type header field. Content-Encoding 
        /// is primarily used to allow a document to be compressed without losing the identity 
        /// of its underlying media type. 
        /// </summary>
        /// <remarks>
        /// http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.5
        /// </remarks>
        private enum HttpContentEncoding
        {
            None,
            Gzip,
            Compress,
            Deflate,
            Unknown
        }

        #endregion
    }
}