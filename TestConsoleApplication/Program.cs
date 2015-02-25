using System;
using MHTMLBuilder;

namespace TestConsoleApplication
{
    internal class ConvertionTesting
    {
        private Builder MHTMLBuilder = new Builder();

        public String createMHTMLFile2(String URL, String fileLocation)
        {
            String path = String.Empty;

            try
            {
                MHTMLBuilder.CreateMHTML(URL, fileLocation);

                //Console.ReadLine();
            }
            catch (CustomException.BuilderInvalidFileNameException e)
            {
                Console.WriteLine("BuilderInvalidFileNameException is " + e.CustomMessage);
            }
            catch (CustomException.BuilderDownLoadHTMLException e)
            {
                Console.WriteLine("BuilderDownLoadHTMLException " + e.CustomMessage);
            }

            return path;
        }


        public String createMHTMLFile(String URL, String fileLocation)
        {
            String path = String.Empty;

            try
            {
                path = MHTMLBuilder.SavePageArchive(fileLocation, Builder.FileStorage.DiskPermanent, URL);
                Console.WriteLine(path);
                //Console.ReadLine();
            }
            catch (CustomException.BuilderInvalidFileNameException e)
            {
                Console.WriteLine("BuilderInvalidFileNameException is " + e.CustomMessage);
            }
            catch (CustomException.BuilderDownLoadHTMLException e)
            {
                Console.WriteLine("BuilderDownLoadHTMLException " + e.CustomMessage);
            }

            return path;
        }

        public String createHTMLFile(String URL, String fileLocation)
        {
            String path = String.Empty;

            try
            {
                path = MHTMLBuilder.SavePageComplete(fileLocation, URL);
                Console.WriteLine(path);
                //Console.ReadLine();
            }
            catch (CustomException.BuilderInvalidFileNameException e)
            {
                Console.WriteLine("BuilderInvalidFileNameException is " + e.CustomMessage);
            }
            catch (CustomException.BuilderDownLoadHTMLException e)
            {
                Console.WriteLine("BuilderDownLoadHTMLException " + e.CustomMessage);
            }

            return path;
        }


        public String SavePageTest(String URL, String fileLocation)
        {
            String path = String.Empty;

            try
            {
                path = MHTMLBuilder.SavePage(fileLocation, URL);
                Console.WriteLine(path);
                //Console.ReadLine();
            }
            catch (CustomException.BuilderInvalidFileNameException e)
            {
                Console.WriteLine("BuilderInvalidFileNameException is " + e.CustomMessage);
            }
            catch (CustomException.BuilderDownLoadHTMLException e)
            {
                Console.WriteLine("BuilderDownLoadHTMLException " + e.CustomMessage);
            }

            return path;
        }

        public String SavePageTextTest(String URL, String fileLocation)
        {
            String path = String.Empty;

            try
            {
                path = MHTMLBuilder.SavePageText(fileLocation, URL);
                Console.WriteLine(path);
            }
            catch (CustomException.BuilderInvalidFileNameException e)
            {
                Console.WriteLine("BuilderInvalidFileNameException is " + e.CustomMessage);
            }
            catch (CustomException.BuilderDownLoadHTMLException e)
            {
                Console.WriteLine("BuilderDownLoadHTMLException " + e.CustomMessage);
            }

            return path;
        }


        public String GetPageArchiveTest(String URL)
        {
            String MHTMLBody = String.Empty;

            try
            {
                MHTMLBody = MHTMLBuilder.GetPageArchive(URL);
                Console.WriteLine(MHTMLBody);
            }
            catch (CustomException.BuilderInvalidFileNameException e)
            {
                Console.WriteLine("BuilderInvalidFileNameException is " + e.CustomMessage);
            }
            catch (CustomException.BuilderDownLoadHTMLException e)
            {
                Console.WriteLine("BuilderDownLoadHTMLException " + e.CustomMessage);
            }

            return MHTMLBody;
        }

        public static void Main(String[] args)
        {
            ConvertionTesting testing = new ConvertionTesting();

            //string mhtml = testing.GetPageArchiveTest("http://www.google.com");

            //Console.WriteLine("MHTML is \n " + mhtml);

            //testing.SavePageTextTest("http://www.google.com", @"C:\Google_SavePageText.txt");
            //testing.SavePageTest("http://www.google.com",@"C:\Google_SavePage.html" );
            //testing.createMHTMLFile2("http://www.google.com", @"C:\FinalTest.mht");
            //testing.createHTMLFile("http://www.google.com", @"C:\RezaSavePageComplete.html");

            //testing.createMHTMLFile2("http://www.google.com", @"C:\NewTests.mht");
            testing.createMHTMLFile("http://www.google.com", @"C:\NewTests.mht");

            //const string testString = @"----=_NextPart_000_00";

            //String s2 =  testString;

            //Console.WriteLine("new line is" + Environment.NewLine);
            //Console.WriteLine("S2 is:" + s2);

            Console.ReadLine();
        }
    }
}