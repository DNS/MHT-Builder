using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace MHTMLBuilder
{
    internal class WebFile
    {
        private Builder _Builder;
        private string _ContentLocation;
        private string _ContentType;
        private byte[] _DownloadedBytes;
        private Exception _DownloadException;
        private string _DownloadExtension = "";
        private string _DownloadFilename = "";
        private string _DownloadFolder = "";
        private NameValueCollection _ExternalFileCollection;

        private bool _IsBinary;

        private System.Text.Encoding _TextEncoding;
        private string _Url;
        private string _UrlFolder;
        private string _UrlRoot;
        private string _UrlUnmodified;
        private bool _UseHtmlFilename;
        private bool _WasDownloaded;

        public Builder.FileStorage Storage = Builder.FileStorage.DiskPermanent;
        public bool WasAppended;


        public WebFile(Builder parent, Builder.FileStorage st, params string[] HTMLData)
        {
            Storage = st;

            _Builder = parent;

            if (HTMLData.Length > 0)
            {
                if (!string.IsNullOrEmpty(HTMLData[0]))
                {
                    setDownLoadedBytes(HTMLData[0]);
                }
            }
        }

        public WebFile(Builder parent, string url, Builder.FileStorage st)
        {
            _Builder = parent;

            if (!string.IsNullOrEmpty(url))
            {
                Url = url;
            }

            Storage = st;
        }

        /// <summary>
        /// The URL target for this file
        /// </summary>
        public string Url
        {
            get { return _Url; }

            set
            {
                _UrlUnmodified = value;
                SetUrl(value, true);
                // ERROR: Not supported in C#: ReDimStatement, find a way to do this
                //ReDim _DownloadedBytes(0)
                _ExternalFileCollection = null;
                _DownloadException = null;
                _TextEncoding = null;
                _ContentType = "";
                _ContentLocation = "";
                _IsBinary = false;
                _WasDownloaded = false;
            }
        }

        /// <summary>
        /// If enabled, will use the first 50 characters of the TITLE tag 
        /// to form the filename when saved to disk
        /// </summary>
        public bool UseHtmlTitleAsFilename
        {
            get { return _UseHtmlFilename; }
            set { _UseHtmlFilename = value; }
        }

        /// <summary>
        /// the folder name used in the DownloadFolder
        /// </summary>
        public string DownloadFolderName
        {
            get { return Regex.Match(DownloadFolder, "(?<Folder>[^\\\\]+)\\\\*$").Groups["Folder"].Value; }
        }


        /// <summary>
        /// folder to download this file to
        /// if no folder is provided, the current application folder will be used
        /// </summary>
        public string DownloadFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_DownloadFolder))
                {
                    _DownloadFolder = AppDomain.CurrentDomain.BaseDirectory;
                }
                return _DownloadFolder;
            }

            set { _DownloadFolder = value; }
        }

        /// <summary>
        /// filename to download this file as
        /// if no filename is provided, a filename will be auto-generated based on
        /// the URL; if the UseHtmlTitleAsFilename property is true, then the
        /// title tag will be used to generate the filename
        /// </summary>
        public string DownloadFilename
        {
            get
            {
                if (string.IsNullOrEmpty(_DownloadFilename))
                {
                    if (_UseHtmlFilename && (WasDownloaded && IsHtml))
                    {
                        string htmlTitle = HtmlTitle;
                        if (!string.IsNullOrEmpty(htmlTitle))
                        {
                            _DownloadFilename = MakeValidFilename(htmlTitle) + ".htm";
                        }
                    }
                    else
                    {
                        _DownloadFilename = FilenameFromUrl();
                    }
                }
                return _DownloadFilename;
            }

            set { _DownloadFilename = value; }
        }

        /// <summary>
        /// fully qualified path and filename to download this file to
        /// TODO: put some try catch for Path.Combine()
        /// </summary>
        public string DownloadPath
        {
            get
            {
                if (string.IsNullOrEmpty(Path.GetExtension(DownloadFilename)))
                {
                    return Path.Combine(DownloadFolder, DownloadFilename + DownloadExtension);
                }
                else
                {
                    return Path.Combine(DownloadFolder, DownloadFilename);
                }
            }

            set
            {
                _DownloadFilename = Path.GetFileName(value);

                if (string.IsNullOrEmpty(_DownloadFilename))
                {
                    _DownloadFolder = value;
                }
                else
                {
                    _DownloadFolder = value.Replace(_DownloadFilename, "");
                }
            }
        }


        /// <summary>
        /// file type extension to use on downloaded file
        /// this property is only used if the DownloadFilename property does not
        /// already contain a file extension
        /// </summary>
        public string DownloadExtension
        {
            get
            {
                if (string.IsNullOrEmpty(_DownloadExtension))
                {
                    if (WasDownloaded)
                    {
                        _DownloadExtension = ExtensionFromContentType();
                    }
                }

                return _DownloadExtension;
            }

            set { _DownloadExtension = value; }
        }

        /// <summary>
        /// If this file has external dependencies, the folder they will be stored on disk
        /// </summary>
        public string ExternalFilesFolder
        {
            get { return Path.Combine(DownloadFolder, Path.GetFileNameWithoutExtension(DownloadFilename)) + "_files"; }
        }

        /// <summary>
        /// The unmodified "raw" URL as originally provided
        /// </summary>
        public String UrlUnmodified
        {
            get { return _UrlUnmodified; }
        }

        /// <summary>
        /// The Content-Location of this URL as provided by the server,
        /// only if the URL was not fully qualified;
        /// eg, http://mywebsite.com/ actually maps to http://mywebsite.com/default.htm 
        /// </summary>
        public string UrlContentLocation
        {
            get { return _ContentLocation; }
        }

        /// <summary>
        /// The root of the URL, eg, http://mywebsite.com/
        /// </summary>
        public string UrlRoot
        {
            get { return _UrlRoot; }
        }

        /// <summary>
        /// The root and folder of the URL, eg, http://mywebsite.com/myfolder
        /// </summary>
        public string UrlFolder
        {
            get { return _UrlFolder; }
        }

        /// <summary>
        /// Was this file successfully downloaded via HTTP?
        /// </summary>
        public bool WasDownloaded
        {
            get { return _WasDownloaded; }
        }

        /// <summary>
        /// The Content-Type of this file as returned by the server
        /// </summary>
        public string ContentType
        {
            get { return _ContentType; }
        }

        /// <summary>
        /// Does this file contain binary data? If not, it must be text data.
        /// </summary>
        public bool IsBinary
        {
            get { return _IsBinary; }
        }


        /// <summary>
        /// The raw bytes returned from the server for this file, this method been modified by reza, read only been removed
        /// I have added the set, just to be able to pass RawHTML to it
        /// </summary>
        public byte[] DownloadedBytes
        {
            get { return _DownloadedBytes; }

            set { _DownloadedBytes = value; }
        }


        /// <summary>
        /// If not .WasDownloaded, the exception that prevented download is stored here
        /// </summary>
        public Exception DownloadException
        {
            get { return _DownloadException; }
        }

        /// <summary>
        /// If this file is text (eg, it isn't binary), the type of text encoding used
        /// </summary>
        public System.Text.Encoding TextEncoding
        {
            get { return _TextEncoding; }
        }

        /// <summary>
        /// Is this file HTML content?
        /// </summary>
        public bool IsHtml
        {
            get { return Regex.IsMatch(_ContentType, "text/html", RegexOptions.IgnoreCase); }
        }


        /// <summary>
        /// Is this file CSS content?
        /// </summary>
        public bool IsCss
        {
            get { return Regex.IsMatch(_ContentType, "text/css", RegexOptions.IgnoreCase); }
        }

        /// <summary>
        /// If this file is HTML, retrieve the &lt;TITLE&gt; tag from the HTML
        /// (maximum of 50 characters)
        /// </summary>
        public string HtmlTitle
        {
            get
            {
                if (!IsHtml)
                {
                    throw new CustomException.ExternalFileHTMLException(
                        "This file isn't HTML, so it has no HTML <TITLE> tag.");
                }
                const int maxLength = 50;

                string s =
                    Regex.Match(ToString(), "<title[^>]*?>(?<text>[^<]+)</title>", RegexOptions.IgnoreCase).Groups[
                        "text"].Value;

                if (s.Length > maxLength)
                {
                    return s.Substring(0, maxLength);
                }
                else
                {
                    return s;
                }
            }
        }

        /// <summary>
        /// Returns a bytes array of a String value, used to convert HTML string to bytes
        /// </summary>
        public byte[] StrToByteArray(string str)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        /// <summary>
        /// Returns a string representation of the data downloaded for this file
        /// </summary>
        public override string ToString()
        {
            if (!_WasDownloaded)
            {
                Download();
            }
            if (!_WasDownloaded)
            {
                return "";
            }
            if ((_DownloadedBytes != null))
            {
                if (_DownloadedBytes.Length > 0)
                {
                    if (_IsBinary)
                    {
                        return "[" + _DownloadedBytes.Length + " bytes of binary data]";
                    }
                    return TextEncoding.GetString(_DownloadedBytes);
                }
                    //-- added by reza to acomodate _DownloadedBytes.Length < 0
                return "";
            }

            return "";
        }

        /// <summary>
        /// Remember to manage the URL and set it to some defualt stuff
        /// </summary>
        public void setDownLoadedBytes(string HTMLData)
        {
            if (!string.IsNullOrEmpty(HTMLData))
            {
                DownloadedBytes = StrToByteArray(HTMLData);

                _WasDownloaded = true;
                _ContentType = "text/html";
                _ContentLocation = " ";
                _TextEncoding = System.Text.Encoding.GetEncoding("Windows-1252");
                //'Me.Url = _ContentLocation

                if (IsHtml)
                {
                    _DownloadedBytes = _TextEncoding.GetBytes(ProcessHtml(ToString()));
                }

                if (IsCss)
                {
                    _DownloadedBytes = _TextEncoding.GetBytes(ProcessCss(ToString()));
                }

                if (Storage != Builder.FileStorage.Memory)
                {
                    SaveToFile();
                }
            }
        }

        /// <summary>
        /// Download this file from the target URL
        /// </summary>
        public void Download()
        {
            Debug.Write("Downloading " + _Url + "  ..");

            DownloadBytes();

            if (_DownloadException == null)
            {
                Debug.WriteLine("OK");
            }
            else
            {
                Debug.WriteLine("failed: ", "Error");
                Debug.WriteLine("    " + _DownloadException.Message, "Error");
                return;
            }

            if (IsHtml)
            {
                Console.WriteLine("IsHTML:Downloading the URL\n ");
                //_DownloadedBytes = _TextEncoding.GetBytes(ProcessHtml(ToString()));
                string html = ProcessHtml(ToString());

                Console.WriteLine("Processed HTML Is: \n " + html);

                _DownloadedBytes = _TextEncoding.GetBytes(html);
            }

            if (IsCss)
            {
                Console.WriteLine("IsCSS:processing CSS ");
                _DownloadedBytes = _TextEncoding.GetBytes(ProcessCss(ToString()));
            }

            if (Storage != Builder.FileStorage.Memory)
            {
                SaveToFile();
            }
        }

        /// <summary>
        /// download this file from the target URL;
        /// place the bytes downloaded in _DownloadedBytes
        /// if an exception occurs, capture it in _DownloadException
        /// </summary>
        private void DownloadBytes()
        {
            if (WasDownloaded) return;

            //-- always download to memory first
            try
            {
                _DownloadedBytes = _Builder.WebClient.DownloadBytes(_Url);
                _WasDownloaded = true;
            }
            catch (System.Net.WebException ex)
            {
                _DownloadException = ex;
                _Builder.WebClient.ClearDownload();
            }

            //-- necessary if the original client URL was imprecise; 
            //-- server location is always authoritatitve
            if (!string.IsNullOrEmpty(_Builder.WebClient.ContentLocation))
            {
                _ContentLocation = _Builder.WebClient.ContentLocation;
                SetUrl(_ContentLocation, false);
            }

            _IsBinary = _Builder.WebClient.ResponseIsBinary;
            _ContentType = _Builder.WebClient.ResponseContentType;
            _TextEncoding = _Builder.WebClient.DetectedEncoding;
            _Builder.WebClient.ClearDownload();
        }

        private void SetUrl(string url, bool validate)
        {
            if (validate)
            {
                _Url = ResolveUrl(url);
            }
            else
            {
                _Url = url;
            }
            //-- http://mywebsite
            _UrlRoot = Regex.Match(url, "http://[^/'\"]+", RegexOptions.IgnoreCase).ToString();
            //-- http://mywebsite/myfolder
            if (_Url.LastIndexOf("/") > 7)
            {
                _UrlFolder = _Url.Substring(0, _Url.LastIndexOf("/"));
            }
            else
            {
                _UrlFolder = _UrlRoot;
            }
        }

        /// <summary>
        /// Pre-process the CSS using global preference settings
        /// </summary>
        private string ProcessCss(string css)
        {
            return ConvertRelativeToAbsoluteRefs(css);
        }

        /// <summary>
        /// Pre-process the HTML using global preference settings
        /// </summary>
        private string ProcessHtml(string html)
        {
            Debug.WriteLine("Downloaded content was HTML/CSS -- processing: resolving URLs, getting <base>, etc");
            if (_Builder.AddWebMark)
            {
                //-- add "mark of the web":
                //-- http://www.microsoft.com/technet/prodtechnol/winxppro/maintain/sp2brows.mspx#XSLTsection133121120120
                if (_Url != null)
                {
                    html = "<!-- saved from url=(" + string.Format("{0:0000}", _Url.Length) + ")" + _Url + " --> " +
                           Environment.NewLine + html;
                }
            }

            //-- see if we need to strip elements from the HTML
            if (_Builder.StripScripts)
            {
                html = StripHtmlTag("script", html);
            }
            if (_Builder.StripIframes)
            {
                html = StripHtmlTag("iframe", html);
            }

            //-- if we have a <base>, we must use it as the _UrlFolder, 
            //-- not what was parsed from the original _Url
            string BaseUrlFolder =
                Regex.Match(html, "<base[^>]+?href=['\"]{0,1}(?<BaseUrl>[^'\">]+)['\"]{0,1}", RegexOptions.IgnoreCase).
                    Groups["BaseUrl"].Value;

            if (!string.IsNullOrEmpty(BaseUrlFolder))
            {
                if (BaseUrlFolder.EndsWith("/"))
                {
                    _UrlFolder = BaseUrlFolder.Substring(0, BaseUrlFolder.Length - 1);
                }
                else
                {
                    _UrlFolder = BaseUrlFolder;
                }
            }

            //-- remove the <base href=''> tag if present; causes problems when viewing locally.
            html = Regex.Replace(html, "<base[^>]*?>", "");

            //-- relative URLs are a PITA for the processing we're about to do, 
            //-- so convert them all to absolute up front
            return ConvertRelativeToAbsoluteRefs(html);
        }

        /// <summary>
        /// converts all relative url references
        ///    href="myfolder/mypage.htm"
        /// into absolute url references
        ///    href="http://mywebsite/myfolder/mypage.htm"
        /// </summary>
        private string ConvertRelativeToAbsoluteRefs(string html)
        {
            Regex r = default(Regex);

            string urlPattern = "(?<attrib>\\shref|\\ssrc|\\sbackground)\\s*?=\\s*?" +
                                "(?<delim1>[\"'\\\\]{0,2})(?!\\s*\\+|#|http:|ftp:|mailto:|javascript:)" +
                                "/(?<url>[^\"'>\\\\]+)(?<delim2>[\"'\\\\]{0,2})";

            string cssPattern = "(?<attrib>@import\\s|\\S+-image:|background:)\\s*?(url)*['\"(]{1,2}" +
                                "(?!http)\\s*/(?<url>[^\"')]+)['\")]{1,2}";

            //-- href="/anything" to href="http://www.web.com/anything"
            r = new Regex(urlPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            html = r.Replace(html, "${attrib}=${delim1}" + _UrlRoot + "/${url}${delim2}");

            //-- href="anything" to href="http://www.web.com/folder/anything"
            r = new Regex(urlPattern.Replace("/", ""), RegexOptions.IgnoreCase | RegexOptions.Multiline);
            html = r.Replace(html, "${attrib}=${delim1}" + _UrlFolder + "/${url}${delim2}");

            //-- @import(/anything) to @import url(http://www.web.com/anything)
            r = new Regex(cssPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            html = r.Replace(html, "${attrib} url(" + _UrlRoot + "/${url})");

            //-- @import(anything) to @import url(http://www.web.com/folder/anything)
            r = new Regex(cssPattern.Replace("/", ""), RegexOptions.IgnoreCase | RegexOptions.Multiline);
            html = r.Replace(html, "${attrib} url(" + _UrlFolder + "/${url})");

            return html;
        }

        /// <summary>
        /// returns a name/value collection of all external files referenced in HTML:
        /// 
        ///     "/myfolder/blah.png"
        ///     'http://mywebsite/blah.gif'
        ///     src=blah.jpg  
        /// 
        /// note that the Key includes the delimiting quotes or parens (if present), but the Value does not
        /// this is important because the delimiters are used for matching and replacement to make the
        /// match more specific!
        /// </summary>
        private NameValueCollection ExternalHtmlFiles()
        {
            //-- avoid doing this work twice, however, be careful that the HTML hasn't
            //-- changed since the last time we called this function
            if ((_ExternalFileCollection != null))
            {
                return _ExternalFileCollection;
            }

            _ExternalFileCollection = new NameValueCollection();
            Regex r = default(Regex);
            string html = ToString();

            Debug.WriteLine("Resolving all external HTML references from URL:");
            Debug.WriteLine("    " + Url);

            //-- src='filename.ext' ; background="filename.ext"
            //-- note that we have to test 3 times to catch all quote styles: '', "", and none
            r =
                new Regex(
                    "(\\ssrc|\\sbackground)\\s*=\\s*((?<Key>'(?<Value>[^']+)')|(?<Key>\"(?<Value>[^\"]+)\")|(?<Key>(?<Value>[^ \\n\\r\\f]+)))",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);
            AddMatchesToCollection(html, r, ref _ExternalFileCollection);

            //-- @import "style.css" or @import url(style.css)
            r =
                new Regex(
                    "(@import\\s|\\S+-image:|background:)\\s*?(url)*\\s*?(?<Key>[\"'(]{1,2}(?<Value>[^\"')]+)[\"')]{1,2})",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);
            AddMatchesToCollection(html, r, ref _ExternalFileCollection);

            //-- <link rel=stylesheet href="style.css">
            r = new Regex("<link[^>]+?href\\s*=\\s*(?<Key>('|\")*(?<Value>[^'\">]+)('|\")*)",
                          RegexOptions.IgnoreCase | RegexOptions.Multiline);
            AddMatchesToCollection(html, r, ref _ExternalFileCollection);

            //-- <iframe src="mypage.htm"> or <frame src="mypage.aspx">
            r = new Regex("<i*frame[^>]+?src\\s*=\\s*(?<Key>['\"]{0,1}(?<Value>[^'\"\\\\>]+)['\"]{0,1})",
                          RegexOptions.IgnoreCase | RegexOptions.Multiline);
            AddMatchesToCollection(html, r, ref _ExternalFileCollection);

            return _ExternalFileCollection;
        }

        /// <summary>
        /// perform the regex replacement of all &lt;tagName&gt; .. &lt;/tagName&gt; blocks
        /// </summary>
        private static string StripHtmlTag(string tagName, string html)
        {
            Regex reg = new Regex(string.Format("<{0}[^>]*?>[\\w|\\t|\\r|\\W]*?</{0}>", tagName),
                                  RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return reg.Replace(html, "");
        }

        /// <summary>
        /// Returns the plain text representation of the data in this file, 
        /// stripping out any HTML tags and codes
        /// </summary>
        public string ToTextString(params bool[] removeWhitespace)
        {
            bool removeWhitSpc = false;

            if (removeWhitespace.Length != 0)
            {
                removeWhitSpc = removeWhitespace[0];
            }

            string html = ToString();
            //-- get rid of <script> .. </script>
            html = StripHtmlTag("script", html);
            //-- get rid of <style> .. </style>
            html = StripHtmlTag("style", html);

            //-- get rid of all HTML tags
            html = Regex.Replace(html, "<\\w+(\\s+[A-Za-z0-9_\\-]+\\s*=\\s*(\"([^\"]*)\"|'([^']*)'))*\\s*(/)*>|<[^>]+>",
                                 " ");
            //-- convert escaped HTML to plaintext
            html = HttpUtility.HtmlDecode(html);

            if (removeWhitSpc)
            {
                //-- clean up whitespace (optional, depends what you want..)
                html = Regex.Replace(html, "[\\n\\r\\f\\t]", " ", RegexOptions.Multiline);
                html = Regex.Replace(html, " {2,}", " ", RegexOptions.Multiline);
            }
            return html;
        }

        /// <summary>
        /// Saves this file to disk as a plain text file
        /// </summary>
        public void SaveAsTextFile()
        {
            SaveToFile(Path.ChangeExtension(DownloadPath, ".txt"), true);
        }

        /// <summary>
        /// Saves this file to disk as a plain text file, to an arbitrary path
        /// </summary>
        public void SaveAsTextFile(string filePath)
        {
            SaveToFile(filePath, true);
        }

        /// <summary>
        /// writes contents of file to DownloadPath, using appropriate encoding as necessary
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(DownloadPath, false);
        }

        /// <summary>
        /// writes contents of file to DownloadPath, using appropriate encoding as necessary
        /// </summary>
        public void SaveToFile(string filePath)
        {
            SaveToFile(filePath, false);
        }

        /// <summary>
        /// sets the DownloadPath and writes contents of file, using appropriate encoding as necessary
        /// </summary>
        ///TODO:Check to see if the _DownLoadedBytes are null or not, importatnt to do so.
        private void SaveToFile(string filePath, bool asText)
        {
            Debug.WriteLine("Saving to file " + filePath);

            Console.WriteLine("Saving to file " + filePath);

            FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate);

            if ((_DownloadedBytes != null))
            {
                try
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    if (IsBinary)
                    {
                        bw.Write(_DownloadedBytes);
                    }
                    else
                    {
                        if (asText)
                        {
                            Console.WriteLine("Saving to file asText");
                            bw.Write(ToTextString());
                        }
                        else
                        {
                            Console.WriteLine("Saving to file by _DownloadedBytes");
                            bw.Write(_DownloadedBytes);
                        }
                    }

                    // added by reza
                    bw.Flush();

                    bw.Close();
                }
                finally
                {
                    if ((fs != null))
                    {
                        fs.Close();
                    }
                }
            }
        }

        /// <summary>
        /// fully resolves any relative pathing inside the URL, and other URL oddities
        /// </summary>
        private string ResolveUrl(string url)
        {
            //-- resolve any relative pathing
            //Console.WriteLine("ResolveUr");

            try
            {
                url = (new Uri(url)).AbsoluteUri;
            }
            catch (UriFormatException ex)
            {
                throw new ArgumentException("'" + url + "' does not appear to be a valid URL.", ex);
            }
            //-- remove any anchor tags from the end of URLs
            if (url.IndexOf("#") > -1)
            {
                string jump = Regex.Match(url, "/[^/]*?(?<jump>#[^/?.]+$)").Groups["jump"].Value;

                if (!string.IsNullOrEmpty(jump))
                {
                    url = url.Replace(jump, "");
                }
            }
            return url;
        }


        /// <summary>
        /// if the user passed in a directory, form the filename automatically using the Html title tag
        /// if the user passed in a filename, make sure the extension matches our desired extension
        /// </summary>
        private string DeriveFilename(string FilePath, string html, string fileExtension)
        {
            if (IsDirectory(FilePath))
            {
                string htmlTitle = HtmlTitle;

                if (string.IsNullOrEmpty(htmlTitle))
                {
                    throw new CustomException.ExternalFileHTMLException(
                        "No filename was provided, and the HTML title tag was not found, " +
                        "so a filename could not be automatically generated. You'll need to provide a filename and not a folder.");
                }

                FilePath = Path.Combine(Path.GetDirectoryName(FilePath), MakeValidFilename(htmlTitle) + fileExtension);
            }
            else
            {
                if (Path.GetExtension(FilePath) != fileExtension)
                {
                    return Path.ChangeExtension(FilePath, fileExtension);
                }
            }

            return FilePath;
        }

        /// <summary>
        /// removes all unsafe filesystem characters to form a valid filesystem filename
        /// TODO: Why there is an if statement on enforcelength? 
        /// </summary>
        private string MakeValidFilename(string s, params bool[] enforceLength)
        {
            bool EnforceLen = false;

            if (enforceLength.Length != 0)
            {
                EnforceLen = enforceLength[0];
            }

            if (EnforceLen)
            {
            }
            //-- replace any invalid filesystem chars, plus leading/trailing/doublespaces
            return Regex.Replace(Regex.Replace(s, "[\\/\\\\\\:\\*\\?\\\"\"\\<\\>\\|]|^\\s+|\\s+$", ""), "\\s{2,}", " ");
        }

        /// <summary>
        /// returns true if this path refers to a directory (vs. a filename)
        /// </summary>
        private bool IsDirectory(string FilePath)
        {
            return FilePath.EndsWith("\\");
        }


        /// <summary>
        /// converts all external Html files (gif, jpg, css, etc) to local refs
        /// external ref:
        ///    &lt;img src="http://mywebsite/myfolder/myimage.gif"&gt;
        /// into local refs:
        ///    &lt;img src="mypage_files/myimage.gif"&gt;
        /// </summary>
        public void ConvertReferencesToLocal()
        {
            Console.WriteLine("ConvertReferencesToLocal()");

            if (!IsHtml & !IsCss)
            {
                throw new CustomException.ExternalFileHTMLException(
                    "Converting references only makes sense for HTML or CSS files; this file is of type '" + ContentType +
                    "'");
            }

            //-- get a list of all external references
            string html = ToString();

            NameValueCollection FileCollection = ExternalHtmlFiles();

            //-- no external refs? nothing to do
            if (FileCollection.Count == 0) return;

            //Dim FolderName As String
            string FileUrl = null;

            foreach (string DelimitedFileUrl in FileCollection.AllKeys)
            {
                FileUrl = FileCollection[DelimitedFileUrl];

                if (_Builder.WebFiles.Contains(FileUrl))
                {
                    WebFile wf = (WebFile)_Builder.WebFiles[FileUrl];

                    string NewPath = ExternalFilesFolder + "/" + wf.DownloadFilename;
                    string DelimitedReplacement = Regex.Replace(DelimitedFileUrl,
                                                                "^(?<StartDelim>\"|'|\\()*(?<Value>[^'\")]*)(?<EndDelim>\"|'|\\))*$",
                                                                "${StartDelim}" + NewPath + "${EndDelim}");

                    //-- correct original Url references in Html so they point to our local files
                    html = html.Replace(DelimitedFileUrl, DelimitedReplacement);
                }
            }

            _DownloadedBytes = _TextEncoding.GetBytes(html);
        }

        /// <summary>
        /// appends key=value named matches in a regular expression
        /// to a target NameValueCollection
        /// </summary>
        private void AddMatchesToCollection(string s, Regex r, ref NameValueCollection nvc)
        {
            string key = null;
            string value = null;

            bool headerDisplayed = false;
            Regex urlRegex = new Regex("^https*://\\w+", RegexOptions.IgnoreCase);

            foreach (Match m in r.Matches(s))
            {
                if (!headerDisplayed)
                {
                    Debug.WriteLine("Matches added from regex:");
                    Debug.WriteLine("\"" + r + "\"");

                    headerDisplayed = true;
                }

                key = m.Groups["Key"].ToString();
                value = m.Groups["Value"].ToString();

                if (nvc[key] == null)
                {
                    Debug.WriteLine(" Match: " + m);
                    Debug.WriteLine("   Key: " + key);
                    Debug.WriteLine(" Value: " + value);

                    if (!urlRegex.IsMatch(value))
                    {
                        Debug.WriteLine("Match discarded; does not appear to be valid fully qualified http:// Url",
                                        "Error");
                    }
                    else
                    {
                        nvc.Add(key, value);
                    }
                }
            }
        }

        /// <summary>
        /// download ALL externally referenced files in this file's html, potentially recursively,
        /// to the default download path for this page
        /// </summary>
        public void DownloadExternalFiles(Builder.FileStorage st, params bool[] recursive)
        {
            bool recrsv = false;

            if (recursive.Length != 0)
            {
                recrsv = recursive[0];
            }

            DownloadExternalFiles(st, ExternalFilesFolder, recrsv);
        }

        /// <summary>
        /// download ALL externally referenced files in this html, potentially recursively
        /// to a specific download path
        /// </summary>
        private void DownloadExternalFiles(Builder.FileStorage st, string targetFolder, bool recursive)
        {
            NameValueCollection FileCollection = ExternalHtmlFiles();

            if (!FileCollection.HasKeys()) return;

            Debug.WriteLine("Downloading all external files collected from URL:");
            Debug.WriteLine("    " + Url);

            foreach (string Key in FileCollection.AllKeys)
            {
                DownloadExternalFile(FileCollection[Key], st, targetFolder, recursive);
            }
        }


        /// <summary>
        /// Download a single externally referenced file (if we haven't already downloaded it)
        /// </summary>
        private void DownloadExternalFile(string url, Builder.FileStorage st, string targetFolder,
                                          params bool[] recursive)
        {
            bool recrsv = false;
            WebFile wf = default(WebFile);
            bool isNew = false;

            if (recursive.Length != 0)
            {
                recrsv = recursive[0];
            }

            //-- have we already downloaded (or attempted to) this file?
            if (_Builder.WebFiles.Contains(url) | _Builder.Url == url)
            {
                wf = (WebFile)_Builder.WebFiles[url];
                isNew = false;
            }
            else
            {
                wf = new WebFile(_Builder, url, st);
                isNew = true;
            }

            //-- if we're planning to store this file on disk, make sure we can
            if (st == Builder.FileStorage.DiskPermanent || st == Builder.FileStorage.DiskTemporary)
            {
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                wf.DownloadFolder = targetFolder;
            }

            wf.Download();

            if (isNew)
            {
                //-- add this (possibly) downloaded file to our shared collection
                _Builder.WebFiles.Add(wf.UrlUnmodified, wf);

                //-- if this is an HTML file, it has dependencies of its own;
                //-- download them into a subfolder
                if ((wf.IsHtml || wf.IsCss) & recrsv)
                {
                    wf.DownloadExternalFiles(st, recrsv);
                }
            }
        }

        /// <summary>
        /// attempt to get a coherent filename out of the Url
        /// </summary>
        private string FilenameFromUrl()
        {
            //-- first, try to get a filename out of the URL itself;
            //-- this means anything past the final slash that doesn't include another slash
            //-- or a question mark, eg http://mywebsite/myfolder/crazy?param=1&param=2
            string filename = Regex.Match(_Url, "/(?<Filename>[^/?]+)[^/]*$").Groups["Filename"].Value;

            if (!string.IsNullOrEmpty(filename))
            {
                //-- that worked, but we need to make sure the filename is unique
                //-- if query params were passed to the URL file
                Uri u = new Uri(_Url);
                if (!string.IsNullOrEmpty(u.Query))
                {
                    filename = Path.GetFileNameWithoutExtension(filename) + "_" + u.Query.GetHashCode() +
                               DownloadExtension;
                }
            }

            //-- ok, that didn't work; if this file is HTML try to get the TITLE tag
            if (string.IsNullOrEmpty(filename))
            {
                if (IsHtml)
                {
                    filename = HtmlTitle;
                    if (!string.IsNullOrEmpty(filename))
                    {
                        filename += ".htm";
                    }
                }
            }

            //-- now we're really desperate. Hash the URL and make that the filename.
            if (string.IsNullOrEmpty(filename))
            {
                filename = _Url.GetHashCode() + DownloadExtension;
            }

            return MakeValidFilename(filename);
        }

        /// <summary>
        /// if we weren't given a filename extension, infer it from the download
        /// Content-Type header
        /// </summary>
        /// <remarks>
        /// http://www.utoronto.ca/webdocs/HTMLdocs/Book/Book-3ed/appb/mimetype.html
        /// </remarks>
        private string ExtensionFromContentType()
        {
            switch (Regex.Match(ContentType, "^[^ ;]+").Value.ToLower())
            {
                case "text/html":
                    return ".htm";
                case "image/gif":
                    return ".gif";
                case "image/jpeg":
                    return ".jpg";
                case "text/javascript":
                case "application/x-javascript":
                    return ".js";
                case "image/x-png":
                    return ".png";
                case "text/css":
                    return ".css";
                case "text/plain":
                    return ".txt";
                default:
                    Debug.WriteLine("Unknown content-type '" + ContentType + "'", "Error");
                    return ".htm";
            }
        }
    }
}