﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using TweetAutomation.LoggingSystem.Business;
using TweetAutomation.UserInterface.BLL;
using TweetAutomation.UserInterface.DataAccessLocal;
using TweetAutomation.UserInterface.DataAccessOnline;
using TweetAutomation.UserInterface.Database;
using TweetAutomation.UserInterface.Factory;
using TweetAutomation.UserInterface.Model;

namespace TweetAutomation.UserInterface
{
  public partial class TweetAutomationFrom : Form
  {
    private const string _tweetRecordsBinaryFilepath = "Tweets.bin";
    private const string _credentialsBinaryFilepath = "Credentials.bin";
    private ILogRepository _logger = LogRepository.LogInstance();
    private ITweetsRepository _tweetsRepository;
    private ITweetRecordFactory _tweetFactory;
    private ISaverBinary _tweetRecordsSaver;
    private ISaverBinary _credentialSaver;
    private IStatusChecker _statusChecker;
    private CredentialsAdapter _adapter;
    private Tweets _dbInstance;
    private TwitterAPIAccess _api;

    public TweetAutomationFrom()
    {
      _logger.Update("DEBUG", "New Tweet Automation instance called.");
      InitializeComponent();
      
      _statusChecker = new StatusChecker();
      _adapter = new CredentialsAdapter();
      _api = new TwitterAPIAccess(_statusChecker, _adapter);

      InitializeDatabase();
      InitializeCustomProperties();

      _tweetsRepository = new TweetsRepository(_dbInstance);
      _tweetFactory = new TweetRecordFactory(_dbInstance);
    }

    private void InitializeDatabase()
    {
      _tweetRecordsSaver = new RecordSaverBinary(_tweetRecordsBinaryFilepath);
      _credentialSaver = new CredentialSaverBinary(_credentialsBinaryFilepath);
      _tweetRecordsSaver.CreateFileIfNotExist();
      _credentialSaver.CreateFileIfNotExist();
      LoadDatabaseInstane();
      UpdateDataGridWithSavedBinary();
      UpdateCredentialsWithSavedBinary();
    }

    private void InitializeCustomProperties()
    {
      TweetDataGrid.AutoGenerateColumns = false;
      DatePicker.Value = DateTime.Today;
      DatePicker.MinDate = DateTime.Today;
      TimePicker.Value = DateTime.Now;

      // Custom event args
      this.Closing += minimizeToTray;
      tweet_automation_notify.MouseClick += restoreWindow;

      #if DEBUG
      loggerText.Visible = true;
      #else
      loggerText.Visible = false;
      #endif
    }

    private void LoadDatabaseInstane()
    {
      _dbInstance = (Tweets)_tweetRecordsSaver.Read<Tweets>();
      if (_dbInstance == null) _dbInstance = Tweets.GetInstance();
    }

    #region All Button Click Event
    private void ExitButtonStripMenuItem(object sender, EventArgs e)
    {
      _logger.Update("ACCESS", "Exit button clicked.");
      Application.Exit();
    }

    private void AboutButtonStripMenuItem(object sender, EventArgs e)
    {
      _logger.Update("ACCESS", "About button clicked.");
      AboutForm about = new AboutForm();
      about.ShowDialog();
    }

    private void ButtonSave(object sender, EventArgs e)
    {
      SaveCredentialToBinary();
    }

    private void ButtonClear(object sender, EventArgs e)
    {
      CredentialsFieldClear();
      // CredentialsFieldUpdate();
      _credentialSaver.DeleteBinaryFile();
    }

    private void ButtonSend(object sender, EventArgs e)
    {
      Tweet tweet = GetTweet();
      SaveCredentialToBinary();
      Sendtweet(tweet);

      _statusChecker.CheckStatus(tweet);
      UpdateDataGridRecord(tweet);
      loggerText.Invoke(new Action(() => loggerText.Text = _dbInstance.Records.Count.ToString()));
      TweetText.Clear();
    }

    private void DeleteButton(object sender, DataGridViewCellEventArgs e)
    {
      _logger.Update("ACCESS", "Deleting Tweet.");
      if (e.RowIndex < 0 || e.ColumnIndex < 0) { return; }
      var buttonValue = TweetDataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value ?? "null";
      if (TweetDataGrid.Columns[e.ColumnIndex].Name != "Delete" && buttonValue.ToString().ToLower() == "delete")
      {
        if (MessageBox.Show(
          "Are you sure want to delete this record ?",
          "Message",
          MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
          int recordID = int.Parse(TweetDataGrid.Rows[e.RowIndex].Cells[0].Value.ToString());
          _tweetsRepository.Delete(recordID);
          _tweetRecordsSaver.UpdateBinary(_dbInstance);
          TweetDataGrid.Rows.Remove(TweetDataGrid.CurrentRow);
        }
      }
    }
    #endregion

    #region Credential

    private Credentials GetCredentials()
    {
      return new Credentials()
      {
        ConsumerKey = ConsumerKey.Text,
        ConsumerSecret = ConsumerSecret.Text,
        AccessTokenKey = AccessTokenKey.Text,
        AccessTokenSecret = AccessTokenSecret.Text
      };
    }
    private void UpdateCredentialsWithSavedBinary()
    {
      _logger.Update("DEBUG", "Updating credential with saved bin.");
      Credentials loadedCredentials = (Credentials)_credentialSaver.Read<TwitterAPIHandler.Model.Credentials>();
      if (loadedCredentials == null) loadedCredentials = new Credentials();
      ConsumerKey.Text = loadedCredentials.ConsumerKey;
      ConsumerSecret.Text = loadedCredentials.ConsumerSecret;
      AccessTokenKey.Text = loadedCredentials.AccessTokenKey;
      AccessTokenSecret.Text = loadedCredentials.AccessTokenSecret;
    }

    private void SaveCredentialToBinary()
    {
      _logger.Update("DEBUG", "Saving credential to bin.");
      _credentialSaver.UpdateBinary(GetCredentials());
    }

    /*private void CredentialsFieldUpdate(Credentials credentials)
    {
      _logger.Update("DEBUG", "Updating credential object.");
      credentials.ConsumerKey = ConsumerKey.Text;
      credentials.ConsumerSecret = ConsumerSecret.Text;
      credentials.AccessTokenKey = AccessTokenKey.Text;
      credentials.AccessTokenSecret = AccessTokenSecret.Text;
    }*/

    private void CredentialsFieldClear()
    {
      _logger.Update("DEBUG", "Clear credential form.");
      ConsumerKey.Clear();
      ConsumerSecret.Clear();
      AccessTokenKey.Clear();
      AccessTokenSecret.Clear();
    }

    #endregion

    #region Tweet Command
    private Tweet GetTweet()
    {
      return _tweetFactory.Create(
        TweetText.Text, DateTime.Now, DateTime.Now,
        SendImmediatelyCheckBox.Checked);
    }

    private void Sendtweet(Tweet tweet)
    {
      _logger.Update("ACCESS", "Sending Tweet.");
      Task.Factory.StartNew(async () =>
      {
        Tweet response = await _api.SendTweet(GetCredentials(), tweet);
        UpdateRecords(response);
      });
    }

    /*private void PlaceRequestOnQueue()
    {
      _logger.Update("DEBUG", "Placing Tweet on Queue table.");
      Tweet record = GetTweet();

      _statusChecker.CheckStatus(record);
      UpdateDataGridRecord(record);

      // SetUpTimerAndSendTweet(edit this);

      TweetText.Clear();
    }*/

    /*private void SendRequestImmediately()
    {
      _logger.Update("ACCESS", "Sending Tweet immediately.");
      ITwitter twtAPI = new Twitter(_credentials);

      Tweet record =
        _tweetFactory.Create(TweetText.Text, DateTime.Now, DateTime.Now);
      _statusChecker.CheckStatusOfSendImmediately(record);
      UpdateDataGridRecord(record);

      if (record.Status != "Starting") return;

      Task.Factory.StartNew(async () =>
      {
        try
        {
          HttpStatusCode response = await SendTweetAsync(twtAPI, record);
          _statusChecker.ChangeStatusByResponse(record, response);
          UpdateRecords(record);
        }
        catch (Exception e)
        {
          _logger.Update("ERROR", e.Message);
        }
      });
    }*/

    /*private void SetUpTimerAndSendTweet(Tweet record)
    {
      try
      {
        _logger.Update("DEBUG", $"Setup timer and sending Tweet. ID: {record.ID}");
        ITwitter twtAPI = new Twitter(_credentials);

        System.Threading.Timer timer;
        TimeSpan timeToGo = record.DateTimeCombined - DateTime.Now;
        if (timeToGo < TimeSpan.Zero) return;

        timer = new System.Threading.Timer(async x =>
        {
          HttpStatusCode response = await SendTweetAsync(twtAPI, record);
          loggerText.Invoke(new Action(() => loggerText.Text = response.ToString()));
          _statusChecker.ChangeStatusByResponse(record, response);
          UpdateRecords(record);
        }, null, timeToGo, System.Threading.Timeout.InfiniteTimeSpan);
      }
      catch (ArgumentOutOfRangeException e)
      {
        _statusChecker.ChangeStatusByResponse(record, HttpStatusCode.Forbidden);
        UpdateRecords(record);
        _logger.Update("ERROR", $"Timer out of range. ID: {record.ID} : {e}");
      }
    }*/

    /*private async Task<HttpStatusCode> SendTweetAsync(
      ITwitter twitterAPI, Tweet record)
    {
      _logger.Update("DEBUG", $"Calling Twitter API. ID: {record.ID}");
      HttpStatusCode response = await twitterAPI.Tweet(record.FullText);
      loggerText.Invoke(new Action(() => loggerText.Text = response.ToString()));

      return response;
    }*/


    #endregion

    #region Data Grid
    private void UpdateDataGridWithSavedBinary()
    {
      _logger.Update("DEBUG", "Updating DataGrid with saved binary.");
      foreach (Tweet tweet in _dbInstance.Records)
      {
        _statusChecker.CheckStatus(tweet);
        Sendtweet(tweet);
        InsertRecordToDataGrid(tweet);

        // SetUpTimerAndSendTweet(record);
      }
    }

    private void UpdateDataGridRecord(Tweet record)
    {
      _logger.Update("DEBUG", $"Updating DataGrid with new record. ID: {record.ID}");
      _tweetsRepository.Append(record);
      _tweetRecordsSaver.UpdateBinary(_dbInstance);

      InsertRecordToDataGrid(record);
    }


    private void InsertRecordToDataGrid(Tweet record)
    {
      _logger.Update("DEBUG", $"Insert record on DataGrid. ID: {record.ID}");
      TweetDataGrid.Rows.Insert(0,
        record.ID, record.FullText, record.DateString,
        record.TimeString, record.Status, "Delete");
    }

    internal void UpdateRecords(Tweet record)
    {
      UpdateStatusOnDataGrid(record);
      _tweetRecordsSaver.UpdateBinary(_dbInstance);
    }

    private void UpdateStatusOnDataGrid(Tweet record)
    {
      _logger.Update("DEBUG", $"Updating status on DataGrid. ID: {record.ID}");
      int rowCount = TweetDataGrid.Rows.Count;
      for (int i = 0; i < rowCount - 1; i++)
      {
        if (TweetDataGrid.Rows[i].Cells[0].Value.ToString() == record.ID.ToString())
        {
          TweetDataGrid.Rows[i].Cells[4].Value = record.Status;
          return;
        }
      }
    }

    #endregion

    #region Tray Icon Control
    private void TrayContextRestore(object sender, EventArgs e)
    {
      tweet_automation_notify.Visible = false;
      this.Show();
    }

    private void TrayContextExit(object sender, EventArgs e)
    {
      this.Close();
    }

    void minimizeToTray(object sender, CancelEventArgs e)
    {
      e.Cancel = true;
      tweet_automation_notify.Visible = true;
      this.Hide();
    }

    void restoreWindow(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Left)
      {
        this.Show();
        tweet_automation_notify.Visible = false;
      }
    }
    #endregion
  }
}
