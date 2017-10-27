using log4net;
using System;

using Should;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class log4net_config_test
    {
        Assembly rootAsm = typeof(log4net_config_test).GetTypeInfo().Assembly;
        [TestMethod]
        public void should_add_global_appender()
        {
            LogManager.ResetConfiguration(rootAsm);
            bool callbackExecuted = false;

            var log = LogManager.GetLogger(typeof(log4net_config_test));
            LogManagerTools.AddCallbackAppender(msg =>
            {
                Console.WriteLine(msg);
                callbackExecuted = true;
            });

            log.Debug("this is a test message");

            callbackExecuted.ShouldBeTrue();
        }

        [TestMethod]
        public void should_add_appender_to_logger()
        {
            LogManager.ResetConfiguration(rootAsm);
            bool callbackExecuted = false;

            var log = LogManager.GetLogger(typeof(log4net_config_test));

            log.AddCallbackAppender(msg =>
            {
                Console.WriteLine(msg);
                callbackExecuted = true;
            });

            log.Debug("this is a test message");

            callbackExecuted.ShouldBeTrue();
        }

        [TestMethod]
        public void should_add_appender_to_logger_level_only()
        {
            LogManager.ResetConfiguration(rootAsm);
            int callbackCount = 0;

            var log = LogManager.GetLogger(typeof(log4net_config_test));
            var rootLog = LogManager.GetLogger(rootAsm, "root");

            log.AddCallbackAppender(msg =>
            {
                Console.WriteLine(msg);
                callbackCount++;
            });

            log.Debug("this is a test message");
            rootLog.Debug("this is a root log message");

            callbackCount.ShouldEqual(1);
        }

        [TestMethod]
        public void root_appender_should_propagate()
        {
            LogManager.ResetConfiguration(rootAsm);
            int callbackCount = 0;

            var rootLog = LogManager.GetLogger(rootAsm, "root");
            rootLog.AddCallbackAppender(msg =>
            {
                Console.WriteLine(msg);
                callbackCount++;
            });
            var log = LogManager.GetLogger(typeof(log4net_config_test));

            log.Debug("this is a test message");
            rootLog.Debug("this is a root log message");

            callbackCount.ShouldEqual(2);
        }
    }
}
