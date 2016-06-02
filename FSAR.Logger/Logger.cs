using System;
using System.IO;
using log4net;
using log4net.Config;

namespace FSAR.Logger
{
    public class Logger
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        private static ILog _logger;

        private Logger()
        {
            _logger = LogManager.GetLogger("FileAppender");
            XmlConfigurator.Configure(new FileInfo("log4net.cfg.xml"));
        }

        public static Logger Instance => _instance.Value;

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Error(string message, Exception exception)
        {
            _logger.Error(message, exception);
        }
    }
}