using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CustomExceptionEnumerators;

namespace MHTMLBuilder
{
    /// <summary>
    /// This class builds the following from a URL:
    ///
    /// .mht file (Web Archive, single file)
    /// .htm file with dereferenced (absolute) references (Web Page, HTML Only)
    /// .htm file plus all referenced files in a local subfolder (Web Page, complete)
    /// .txt file (non-HTML contents of Web Page)
    ///
    /// The .mht format is based on RFC2557
    /// "compliant Multipart MIME Message (mhtml web archive)"
    /// http://www.ietf.org/rfc/rfc2557.txt
    /// </summary>
    /// <remarks>
    /// Jeff Atwood
    /// http://www.codinghorror.com/
    /// </remarks>
    public class Builder
    {
        #region FileStorage enum

        public enum FileStorage
        {
            Memory,
            DiskPermanent,
            DiskTemporary
        }

        #endregion

        private const string _MimeBoundaryTag = @"----=_NextPart_000_00";

        private bool _AddWebMark = true;
        private bool _AllowRecursion = true;

        private WebFile _HtmlFile;
        private StringBuilder _MhtBuilder;


        internal WebClientEx WebClient = new WebClientEx();
        internal SortedList WebFiles = new SortedList();

        public Builder()
        {
            _HtmlFile = new WebFile(this, FileStorage.Memory);
        }

        #region "  Properties"

        /// <summary>
        /// Specifies the target Url we want to save
        /// </summary>
        public string Url
        {
            get { return _HtmlFile.Url; }

            set
            {
                WebFiles.Clear();
                _HtmlFile.Url = value;
            }
        }

        /// <summary>
        /// returns the Mime content-type string designation of a mht file
        /// </summary>
        public string MhtContentType
        {
            get { return "message/rfc822"; }
        }


        /// <summary>
        /// *only* set this if you want to FORCE a specific text encoding for all the HTML pages you're downloading;
        /// otherwise the text encoding is autodetected, which is generally what you want
        /// </summary>
        public Encoding TextEncoding { get; set; }

        /// <summary>
        /// Add the "Mark of the web" to retrieved HTML content so it can run 
        /// locally on Windows XP SP2
        /// </summary>
        /// <remarks>
        ///   http://www.microsoft.com/technet/prodtechnol/winxppro/maintain/sp2brows.mspx#XSLTsection133121120120
        /// </remarks>
        public bool AddWebMark
        {
            get { return _AddWebMark; }
            set { _AddWebMark = value; }
        }


        /// <summary>
        /// Strip all &lt;SCRIPT&gt; blocks from any retrieved HTML
        /// </summary>
        public bool StripScripts { get; set; }


        /// <summary>
        /// Strip all &lt;IFRAME&gt; blocks from any retrieved HTML
        /// </summary>
        public bool StripIframes { get; set; }

        /// <summary>
        /// The browser identification string that is sent in all HTTP requests;
        /// using a different string can produce simplified (downlevel) HTML
        /// </summary>
        /// <remarks>
        /// defaults to browser ID string of vanilla IE6 as seen in XP SP2
        /// </remarks>
        public string BrowserIdString
        {
            get { return WebClient.BrowserIdString; }

            set { WebClient.BrowserIdString = value; }
        }


        /// <summary>
        /// the target URL requires authentication
        /// if not provided, the current user's credentials will automatically be sent
        /// </summary>
        public bool AuthenticationRequired
        {
            get { return WebClient.AuthenticationRequired; }
            set { WebClient.AuthenticationRequired = value; }
        }


        /// <summary>
        /// HTTP Authentication user for Url
        /// if left blank, the current user's credentials will be sent
        /// </summary>
        public string AuthenticationUser
        {
            get { return WebClient.AuthenticationUser; }
            set { WebClient.AuthenticationUser = value; }
        }


        /// <summary>
        /// HTTP Authentication password for Url
        /// if left blank, the current user's credentials will be sent
        /// </summary>
        public string AuthenticationPassword
        {
            get { return WebClient.AuthenticationPassword; }
            set { WebClient.AuthenticationPassword = value; }
        }


        /// <summary>
        /// HTTP proxy username
        /// if left blank, the current user's credentials will be sent
        /// </summary>
        public string ProxyUser
        {
            get { return WebClient.ProxyUser; }
            set { WebClient.ProxyUser = value; }
        }

        /// <summary>
        /// HTTP proxy password
        /// if left blank, the current user's credentials will be sent
        /// </summary>
        public string ProxyPassword
        {
            get { return WebClient.ProxyPassword; }
            set { WebClient.ProxyPassword = value; }
        }


        /// <summary>
        /// HTTP proxy URL
        /// if provided, proxy will always be used; if left blank, proxy will not be used
        /// </summary>
        public string ProxyUrl
        {
            get { return WebClient.ProxyUrl; }
            set { WebClient.ProxyUrl = value; }
        }


        /// <summary>
        /// Proxy requires authentication
        /// if not provided, the current user's credentials will automatically be sent
        /// </summary>
        public bool ProxyAuthenticationRequired
        {
            get { return WebClient.ProxyAuthenticationRequired; }
            set { WebClient.ProxyAuthenticationRequired = value; }
        }


        /// <summary>
        /// allow recursive retrieval of any embedded HTML (typically IFRAME or FRAME)
        /// turn off to prevent infinite recursion in the case of pages that reference themselves..
        /// </summary>
        public bool AllowRecursiveFileRetrieval
        {
            get { return _AllowRecursion; }
            set { _AllowRecursion = value; }
        }

        #endregion "  Properties"

        #region "Public"

        /// <summary>
        /// Saves URL to disk as a single HTML file, modified with absolute external references
        /// if a folder is provided instead of a filename, the TITLE tag is used to name the file
        /// </summary>
        /// <param name="outputFilePath">path to generate to, or filename to generate</param>
        /// <param name="url">fully qualified URL you wish to save</param>
        /// <returns>the complete path of the HTML file that was saved to disk</returns>
        public string SavePage(String outputFilePath, params String[] url)
        {
            String URL = String.Empty;

            if (url.Length > 0)
                URL = url[0];


            ValidateFilename(outputFilePath, ".htm;.html");

            if (URL != null)
            {
                DownloadHtmlFile(URL);
            }

            _HtmlFile.UseHtmlTitleAsFilename = true;
            _HtmlFile.DownloadPath = outputFilePath;
            _HtmlFile.SaveToFile();

            return _HtmlFile.DownloadPath;
        }


        /// <summary>
        /// Saves URL to disk as a plain text file, stripping all HTML from it
        /// if a folder is provided instead of a filename, the TITLE tag is used to name the file
        /// </summary>
        /// <param name="outputFilePath">path to generate to, or filename to generate</param>
        /// <param name="url">fully qualified URL you wish to save</param>
        /// <returns>the complete path of the text file that was saved to disk</returns>
        public string SavePageText(String outputFilePath, params String[] url)
        {
            String URL = String.Empty;

            if (url.Length > 0)
                URL = url[0];

            ValidateFilename(outputFilePath, ".txt");

            if (URL != null)
            {
                DownloadHtmlFile(URL);
            }

            _HtmlFile.UseHtmlTitleAsFilename = true;
            _HtmlFile.DownloadPath = outputFilePath;
            _HtmlFile.SaveAsTextFile();
            return Path.ChangeExtension(_HtmlFile.DownloadPath, ".txt");
        }


        /// <summary>
        /// Saves URL to disk as multiple files: a single HTML file, modified with local references
        /// to externally referenced files in a subfolder
        /// if a folder is provided instead of a filename, the TITLE tag is used to name the file
        /// </summary>
        /// <param name="outputFilePath">path to generate to, or filename to generate</param>
        /// <param name="url">fully qualified URL you wish to save</param>
        /// <returns>the complete path of the HTML file that was saved to disk</returns>
        public string SavePageComplete(string outputFilePath, params String[] url)
        {
            String URL = String.Empty;

            if (url.Length > 0)
                URL = url[0];

            ValidateFilename(outputFilePath, ".htm;.html");

            if (URL != null)
            {
                DownloadHtmlFile(URL);
            }


            //-- first, let's get all the external files
            _HtmlFile.DownloadPath = outputFilePath;
            _HtmlFile.UseHtmlTitleAsFilename = true;
            _HtmlFile.DownloadExternalFiles(FileStorage.DiskPermanent, _AllowRecursion);

            //-- convert any references in external files
            foreach (DictionaryEntry de in WebFiles)
            {
                WebFile ef = (WebFile)de.Value;
                if (ef.IsHtml | ef.IsCss)
                {
                    ef.ConvertReferencesToLocal();
                    ef.SaveToFile();
                }
            }

            //-- convert the main HTML references
            _HtmlFile.ConvertReferencesToLocal();
            _HtmlFile.SaveToFile();

            return _HtmlFile.DownloadPath;
        }


        /// <summary>
        /// Generates a string representation of the URL as a Mht archive file
        /// using exclusively in-memory storage
        /// </summary>
        /// <param name="url">fully qualified URL you wish to render to Mht</param>
        /// <returns>string representation of MHT file</returns>
        public string GetPageArchive(params String[] url)
        {
            String URL = String.Empty;

            if (url.Length > 0)
                URL = url[0];

            // check this to see wotDownloadHtmlFile(URL)does if URL == null
            if (URL != null)
            {
                DownloadHtmlFile(URL);
            }

            _HtmlFile.DownloadExternalFiles(FileStorage.Memory, _AllowRecursion);
            AppendMhtHeader(_HtmlFile);
            AppendMhtFiles();
            return FinalizeMht();
        }

        public void CreateMHTML(String url, string filePath)
        {
            FileStream MHTMLFile = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            StreamWriter writer = new StreamWriter(MHTMLFile);

            writer.Write(GetPageArchive(url));
            writer.Flush();

            MHTMLFile.Flush();
            writer.Close();
        }

        /// <summary>
        /// Saves URL to disk as a single file Mht archive
        /// if a folder is provided instead of a filename, the TITLE tag is used to name the file
        /// </summary>
        /// <param name="outputFilePath">path to generate to, or filename to generate</param>
        /// <param name="st">type of storage to use when generating the Mht archive</param>
        /// <param name="url">fully qualified URL you wish to save as Mht</param>
        /// <returns>the complete path of the Mht archive file that was generated</returns>
        public string SavePageArchive(String outputFilePath, FileStorage st, params String[] url)
        {
            String URL = String.Empty;

            if (url.Length > 0)
                URL = url[0];

            ValidateFilename(outputFilePath, ".mht");

            // check this to see wotDownloadHtmlFile(URL)does if URL == null
            if (URL != null)
            {
                DownloadHtmlFile(URL);
            }

            _HtmlFile.DownloadPath = outputFilePath;
            _HtmlFile.UseHtmlTitleAsFilename = true;

            //-- if set to permanent disk storage, make a local copy of the HTML
            if (st == FileStorage.DiskPermanent)
            {
                _HtmlFile.SaveToFile(Path.ChangeExtension(_HtmlFile.DownloadPath, ".htm"));
            }

            //-- download all references
            _HtmlFile.DownloadExternalFiles(st, _AllowRecursion);

            //-- build the Mht 
            AppendMhtHeader(_HtmlFile);
            AppendMhtFiles();
            FinalizeMht(Path.ChangeExtension(_HtmlFile.DownloadPath, ".mht"));

            //-- possibly destroy temporary resources
            if (st == FileStorage.DiskTemporary)
            {
                foreach (DictionaryEntry de in WebFiles)
                {
                    WebFile ef = (WebFile)de.Value;

                    if (ef.Storage == FileStorage.DiskTemporary)
                    {
                        File.Delete(ef.DownloadPath);
                    }
                    //-- if the temp folder is empty, kill that too
                    if (Directory.GetFileSystemEntries(ef.DownloadFolder).Length == 0)
                    {
                        Directory.Delete(ef.DownloadFolder);
                    }
                }
            }

            WebFiles.Clear();

            return Path.ChangeExtension(_HtmlFile.DownloadPath, ".mht");
        }


        public string ConvertHTMLToMHTML(String HTMLString, String outputFilePath, FileStorage st)
        {
            _HtmlFile.setDownLoadedBytes(HTMLString);


            ValidateFilename(outputFilePath, ".mht");

            _HtmlFile.DownloadPath = outputFilePath;
            _HtmlFile.UseHtmlTitleAsFilename = true;


            //-- if set to permanent disk storage, make a local copy of the HTML
            if (st == FileStorage.DiskPermanent)
            {
                _HtmlFile.SaveToFile(Path.ChangeExtension(_HtmlFile.DownloadPath, ".htm"));
            }

            //-- download all references
            _HtmlFile.DownloadExternalFiles(st, _AllowRecursion);

            //-- build the Mht 
            AppendMhtHeader(_HtmlFile);
            AppendMhtFiles();
            FinalizeMht(Path.ChangeExtension(_HtmlFile.DownloadPath, ".mht"));

            //-- possibly destroy temporary resources
            if (st == FileStorage.DiskTemporary)
            {
                foreach (DictionaryEntry de in WebFiles)
                {
                    WebFile ef = (WebFile)de.Value;
                    if (ef.Storage == FileStorage.DiskTemporary)
                    {
                        File.Delete(ef.DownloadPath);
                    }
                    //-- if the temp folder is empty, kill that too
                    if (Directory.GetFileSystemEntries(ef.DownloadFolder).Length == 0)
                    {
                        Directory.Delete(ef.DownloadFolder);
                    }
                }
            }
            WebFiles.Clear();

            return Path.ChangeExtension(_HtmlFile.DownloadPath, ".mht");
        }

        #endregion "Public"

        #region "  Private"

        /// <summary>
        /// returns the root HTML we'll use to generate everything else;
        /// this is tracked in the _HtmlFile object, which is always FileStorage.Memory
        /// </summary>
        private bool DownloadHtmlFile(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Url = url;
            }

            _HtmlFile.Storage = FileStorage.Memory;
            _HtmlFile.WasAppended = false;

            _HtmlFile.Download();

            if (!_HtmlFile.WasDownloaded)
            {
                throw new CustomException.BuilderDownLoadHTMLException(Url, _HtmlFile.DownloadException.Message,
                                                                       _HtmlFile.DownloadException);
            }

            return true;
        }

        /// <summary>
        /// returns true if this path refers to a directory (vs. a filename)
        /// </summary>
        private bool IsDirectory(string FilePath)
        {
            return FilePath.EndsWith("\\");
        }


        /// <summary>
        /// ensures that the path, if it contains a filename, matches one of the semicolon delimited 
        /// filetypes provided in fileExtension
        /// </summary>
        private bool ValidateFilename(string FilePath, string fileExtensions)
        {
            bool validate = false;

            if (IsDirectory(FilePath)) validate = true;

            string ext = Path.GetExtension(FilePath);

            if (string.IsNullOrEmpty(ext))
            {
                throw new CustomException.BuilderInvalidFileNameException(Path.GetFileName(FilePath), fileExtensions,
                                                                          InvalidFileNameExceptionType.InvalidFileName);
            }

            if (!Regex.IsMatch(fileExtensions, ext + "(;|$)", RegexOptions.IgnoreCase))
            {
                throw new CustomException.BuilderInvalidFileNameException(Path.GetFileName(FilePath), fileExtensions,
                                                                          InvalidFileNameExceptionType.InvalideExtension);
            }


            return validate;
        }

        /// <summary>
        /// removes all unsafe filesystem characters to form a valid filesystem filename
        /// </summary>
        private string MakeValidFilename(string s)
        {
            //-- replace any invalid filesystem chars with underscore
            return Regex.Replace(s, "[\\/\\\\\\:\\*\\?\\\"\\<\\>\\|]", "_");
        }


        /// <summary>
        /// appends all downloaded files (from _ExternalFiles) to our MhtBuilder
        /// </summary>
        private void AppendMhtFiles()
        {
            foreach (DictionaryEntry de in WebFiles)
            {
                WebFile ef = (WebFile)de.Value;
                AppendMhtFile(ef);
            }
            AppendMhtBoundary();
        }


        /// <summary>
        /// appends the Mht header, which includes the root HTML
        /// </summary>
        private void AppendMhtHeader(WebFile ef)
        {
            //-- clear the stringbuilder contents
            _MhtBuilder = new StringBuilder();

            AppendMhtLine("From: <Saved by " + Environment.UserName + " on " + Environment.MachineName + ">");
            AppendMhtLine("Subject: " + ef.HtmlTitle);
            AppendMhtLine("Date: " + DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz"));
            AppendMhtLine("MIME-Version: 1.0");
            AppendMhtLine("Content-Type: multipart/related;");
            AppendMhtLine(Convert.ToChar(9) + "type=\"text/html\";");
            AppendMhtLine(Convert.ToChar(9) + "boundary=\"" + _MimeBoundaryTag + "\"");
            AppendMhtLine("X-MimeOLE: Produced by " + GetType() + " " +
                          Assembly.GetExecutingAssembly().GetName().Version);
            AppendMhtLine(string.Empty);
            AppendMhtLine("This is a multi-part message in MIME format.");

            AppendMhtFile(ef);
        }

        /// <summary>
        /// append a single line, with trailing CRLF, to our MhtBuilder
        /// </summary>
        private void AppendMhtLine(params string[] s)
        {
            if (s.Length > 0)
            {
                //if (!string.IsNullOrEmpty(s[0]))
                //{
                _MhtBuilder.Append(s[0]);
                _MhtBuilder.Append(Environment.NewLine);
                Console.WriteLine(_MhtBuilder.ToString());
                //Console.ReadLine();
                //}
            }
        }


        /// <summary>
        /// appends a boundary marker to our MhtBuilder
        /// </summary>
        private void AppendMhtBoundary()
        {
            // I have replaced " ", with "". look into this later.
            AppendMhtLine(string.Empty);
            AppendMhtLine("--" + _MimeBoundaryTag);
            //AppendMhtLine(" ");
        }

        /// <summary>
        /// Appends a downloaded external file to our MhtBuilder
        /// </summary>
        private void AppendMhtFile(WebFile ef)
        {
            if (ef.WasDownloaded & !ef.WasAppended)
            {
                if (ef.IsBinary)
                {
                    AppendMhtBinaryFile(ef);
                }
                else
                {
                    AppendMhtTextFile(ef);
                }
            }
            ef.WasAppended = true;
        }


        /// <summary>
        /// Appends a downloaded external text file to our MhtBuilder using Quoted-Printable encoding
        /// </summary>
        private void AppendMhtTextFile(WebFile ef)
        {
            AppendMhtBoundary();
            AppendMhtLine("Content-Type: " + ef.ContentType + ";");
            AppendMhtLine(Convert.ToChar(9) + "charset=\"" + ef.TextEncoding.WebName + "\"");
            AppendMhtLine("Content-Transfer-Encoding: quoted-printable");
            AppendMhtLine("Content-Location: " + ef.Url);
            AppendMhtLine(String.Empty);
            AppendMhtLine(QuotedPrintableEncode(ef.ToString(), ef.TextEncoding));
        }


        /// <summary>
        /// Appends a downloaded external binary file to our MhtBuilder using Base64 encoding
        /// </summary>
        private void AppendMhtBinaryFile(WebFile ef)
        {
            AppendMhtBoundary();
            AppendMhtLine("Content-Type: " + ef.ContentType);
            AppendMhtLine("Content-Transfer-Encoding: base64");
            AppendMhtLine("Content-Location: " + ef.Url);
            AppendMhtLine(String.Empty);

            //-- note that chunk size is equal to maximum line width (expanded = 75 chars)
            const int ChunkSize = 57;

            if (ef.Storage == FileStorage.Memory)
            {
                int len = ef.DownloadedBytes.Length;
                if (len <= ChunkSize)
                {
                    AppendMhtLine(Convert.ToBase64String(ef.DownloadedBytes, 0, len));
                }
                else
                {
                    int i = 0;
                    while (i + ChunkSize < len)
                    {
                        AppendMhtLine(Convert.ToBase64String(ef.DownloadedBytes, i, ChunkSize));
                        i += ChunkSize;
                    }
                    if (i != len)
                    {
                        AppendMhtLine(Convert.ToBase64String(ef.DownloadedBytes, i, len - i));
                    }
                }
            }
            else
            {
                //--Initialized to Nothing by Reza
                FileStream fs = null;

                byte[] b = new byte[ChunkSize + 1];

                int BytesRead = 0;

                try
                {
                    fs = new FileStream(ef.DownloadPath, FileMode.Open, FileAccess.Read);
                    BytesRead = fs.Read(b, 0, ChunkSize);
                    while (BytesRead > 0)
                    {
                        AppendMhtLine(Convert.ToBase64String(b, 0, BytesRead));
                        BytesRead = fs.Read(b, 0, ChunkSize);
                    }
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
        /// dumps our MhtBuilder to disk and clears it
        /// </summary>
        private void FinalizeMht(string outputFilePath)
        {
            try
            {
                Console.WriteLine("FinalizeMht TextEncoding is:\n " + _HtmlFile.TextEncoding);
                Console.WriteLine("FinalizeMht OutputFilePath is:\n " + outputFilePath);

                StreamWriter sr = new StreamWriter(outputFilePath, false, _HtmlFile.TextEncoding);

                //Console.WriteLine("Dumping the _MhtBuilder \n " + _MhtBuilder.ToString());

                sr.Write(_MhtBuilder.ToString());
                sr.Flush();
                sr.Close();
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("FinalizeMht():UnauthorizedAccessException \n " + ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("FinalizeMht():ArgumentException \n " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("FinalizeMht():ArgumentException \n " + ex.Message);
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine("FinalizeMht():ArgumentException \n " + ex.Message);
            }
            catch (PathTooLongException ex)
            {
                Console.WriteLine("FinalizeMht():IOException\n " + ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine("FinalizeMht():IOException\n " + ex.Message);
            }
            catch (System.Security.SecurityException ex)
            {
                Console.WriteLine("FinalizeMht():SecurityException\n " + ex.Message);
            }
            finally
            {
                _MhtBuilder = null;
            }
        }


        /// <summary>
        /// dumps our MhtBuilder as a string and clears it
        /// </summary>
        private string FinalizeMht()
        {
            string s = _MhtBuilder.ToString();
            //Console.WriteLine("FinalizeMht():_MhtBuilder null " + s);
            _MhtBuilder = null;
            return s;
        }

        #endregion "Private"

        #region "Quoted-Printable encoding"

        /// <summary>
        /// converts a string into Quoted-Printable encoding
        ///   http://www.freesoft.org/CIE/RFC/1521/6.htm
        /// </summary>
        private static string QuotedPrintableEncode(string s, Encoding e)
        {
            int Ascii = 0;
            int LastSpace = 0;
            int LineLength = 0;
            int LineBreaks = 0;
            StringBuilder sb = new StringBuilder();
            //Dim longchar As String

            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            foreach (char c in s)
            {
                Ascii = Convert.ToInt32(c);

                if (Ascii == 61 | Ascii > 126)
                {
                    if (Ascii <= 255)
                    {
                        sb.Append("=");
                        sb.Append(Convert.ToString(Ascii, 16).ToUpper());
                        LineLength += 3;
                    }
                    else
                    {
                        //-- double-byte land..Reza added c.toString()
                        foreach (byte b in e.GetBytes(c.ToString()))
                        {
                            sb.Append("=");
                            sb.Append(Convert.ToString(b, 16).ToUpper());
                            LineLength += 3;
                        }
                    }
                }
                else
                {
                    sb.Append(c);
                    LineLength += 1;
                    if (Ascii == 32) LastSpace = sb.Length;
                }

                if (LineLength >= 73)
                {
                    if (LastSpace == 0)
                    {
                        sb.Insert(sb.Length, "=" + Environment.NewLine);
                        LineLength = 0;
                    }
                    else
                    {
                        sb.Insert(LastSpace, "=" + Environment.NewLine);
                        LineLength = sb.Length - LastSpace - 1;
                    }
                    LineBreaks += 1;
                    LastSpace = 0;
                }
            }

            //-- if we split the line, have to indicate trailing spaces
            if (LineBreaks > 0)
            {
                if (Equals(sb[sb.Length - 1], " "))
                {
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("=20");
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}