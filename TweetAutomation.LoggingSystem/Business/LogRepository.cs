﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TweetAutomation.LoggingSystem.BusinessLogic
{
  public class LogRepository : ILogRepository
  {
    private const string _loggerFileName = "TweetAutomation.log";
    private static object _instanceLocker = new object(); // threadsafe
    private static object _updateLocker = new object(); // threadsafe
    private static LogRepository _repository;
    private LogRepository()
    {
      if (!CheckFileExist()) Create();
    }
    public static LogRepository LogInstance()
    {
      if (_repository == null)
      {
        // threadsafe
        lock (_instanceLocker)
        {
          if (_repository == null)
          {
            _repository = new LogRepository();
          }
        }
      }
      return _repository;
    }

    public void Create()
    {
      using (File.Create(_loggerFileName))
      {

      }
    }

    public void Read()
    {
      throw new NotImplementedException();
    }

    public void Update(string status, string log)
    {
      lock (_updateLocker)
      {
        string loggerQuery = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ff tt")
        + " | " + status + " | " + log;
        using (StreamWriter file = new StreamWriter(_loggerFileName, append: true))
        {
          file.WriteLine(loggerQuery);
        }
      }
    }

    public void Delete()
    {
      File.Delete(_loggerFileName);
    }

    private bool CheckFileExist()
    {
      return File.Exists(_loggerFileName);
    }
  }
}
