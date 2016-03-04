using CodeEndeavors.ServiceHost.Common.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Logger = CodeEndeavors.ServiceHost.Common.Services.Logging;

namespace CodeEndeavors.Services.ResourceManager.Client
{
    //we are no longer exposing ANYTHING from CodeEndeavors.ServiceHost.Common outside of client
    //so this needs to be brought inhouse - YES it is duplicated for each service
    public class ClientCommandResult<TData>
    {
        private TData _data;
        public TimeSpan ExecutionTime;
        public TimeSpan ServerExecutionTime;
        public string StatusMessage;
        //public string LoggerKey;
        public bool Success;
        private List<string> _errors;
        private List<string> _messages;
        private Stopwatch _watch;
        public TData Data
        {
            get { return this._data; }
            set { this._data = value; }
        }
        public List<string> Messages
        {
            get { return this._messages; }
        }
        public List<string> Errors
        {
            get { return this._errors; }
        }
        public ClientCommandResult()
            : this(true)
        {
        }
        public ClientCommandResult(bool startTimer)
        {
            this._messages = new List<string>();
            this._errors = new List<string>();
            this._watch = new Stopwatch();
            if (startTimer)
            {
                this.StartTimer();
            }
        }
        public void ReportResult(TData data, bool success)
        {
            this.Success = success;
            this.Data = data;
            this.StopTimer();
            if (Logger.IsDebugEnabled)
                Logger.Debug(this.ToString());
        }
        public void ReportResult(ServiceResult<TData> result, bool success)
        {
            this.Success = result.Success;
            this.Data = result.Data;
            this.ServerExecutionTime = new TimeSpan(0, 0, 0, (int)Math.Round(result.ExecutionTime / 1000.0), (int)Math.Round(result.ExecutionTime % 1000.0));
            this.Errors.AddRange(result.Errors);
            this.StopTimer();
            if (Logger.IsDebugEnabled)
                Logger.Debug(this.ToString());
        }
        public void ReportResult(ClientCommandResult<TData> result, bool success)
        {
            this.Success = result.Success;
            this.Data = result.Data;
            this.ServerExecutionTime = result.ServerExecutionTime;
            this.Errors.AddRange(result.Errors);
            this.StopTimer();
            if (Logger.IsDebugEnabled)
                Logger.Debug(this.ToString());
        }
        public void ReportResult<T2>(ClientCommandResult<T2> result, TData data, bool success)
        {
            this.Success = result.Success;
            this.Data = data;
            this.ServerExecutionTime = result.ServerExecutionTime;
            this.Errors.AddRange(result.Errors);
            this.StopTimer();
            if (Logger.IsDebugEnabled)
                Logger.Debug(this.ToString());
        }
        public void AddException(Exception ex)
        {
            Logger.Error(ex.ToString());
            this.Errors.Add(ex.ToString());
        }
        public void StartTimer()
        {
            this._watch.Start();
        }
        public void StopTimer()
        {
            this._watch.Stop();
            this.ExecutionTime = this._watch.Elapsed;
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("Success: {0}   Time: {1}   Server Time: {2}", this.Success, this.ExecutionTime, this.ServerExecutionTime));
            if (this.Errors.Count > 0)
            {
                foreach (var error in this.Errors)
                    sb.AppendLine(string.Format("ERROR: {0}", error));
            }
            return sb.ToString();
        }

        public static ClientCommandResult<TData> Execute(Action<ClientCommandResult<TData>> codeFunc) 
        {
            var result = new ClientCommandResult<TData>(true);
            try
            {
                codeFunc.Invoke(result);
            }
            catch (Exception ex)
            {
                result.AddException(ex);
            }
            finally
            {
                result.StopTimer();
            }
            return result;
        }

    }

}
