using System;

namespace CustomExceptionEnumerators
{
    /// <summary>
    ///Severity level of Exception\
    ///The Severity level determines the criticality of the error
    /// </summary>
    public enum SeverityLevel
    {
        Fatal,
        Critical,
        Information
    }

    /// <summary>
    ///Log level of Exception
    ///The Loglevel determines whether an entry needs to be made in Log. Based on the log level chosen , 
    ///entries can be made either in the Debug Log or System Event Log .
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Event
    }

    /// <summary>
    ///Invalid File Name exception types
    ///We have Invalid File Name and also Invalid Extension
    /// </summary>
    public enum InvalidFileNameExceptionType
    {
        InvalidFileName,
        InvalideExtension
    }

    /// <summary>
    ///EmailJobHistoryStatus that we have set in the DB's EmailJobHistoryStatus
    /// table.
    ///  Sent = "Sent": email Job has been relayed to the smtp server successfully
    ///  Resend = "Resend": EmailJob is bein resent sucessfully.
    ///  Failed = "Failed": Failed to send the eamilJob and there has been an issue.
    /// </summary>
    public struct EmailJobHistoryStatus
    {
        public const String Sent = "Sent";
        public const String Resend = "Resend";
        public const String Failed = "Failed";

    }

    /// <summary>
    ///  EmailJobStatus that we have set in the DB's EmailJobStatus
    ///  table.
    ///  New = "New": The EmailJob has been submitted and all ready to be sent out and processed.
    ///  Processed = "Processed": EmailJob has been processed by MessagingWindows Service, 
    ///  this does not neccessarily mean its been sent successfully as well.
    ///  Pending = "Pending": EmailJob is pending and not ready to be sent out yet, it wont be 
    ///  proccessed by MessagingWindowsService.
    /// </summary>

    public struct EmailJobStatus
    {
        public const String New = "New";
        public const String Processed = "Processed";
        public const String Pending = "Pending";
    }
}