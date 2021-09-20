﻿using System;
using UserInterface.Model;

namespace UserInterface.Factory
{
  public interface ITweetRecordFactory
  {
    TweetRecord Create(string tweet, DateTime date, DateTime time);
  }
}
