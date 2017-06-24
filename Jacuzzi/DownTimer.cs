using System;

namespace Jacuzzi
{
	public class DownTimer : IDisposable
	{
		public uint Time
		{
            get => (uint)_timeOut.Milliseconds;
            set { _timeOut = TimeSpan.FromMilliseconds(value); }
		}

        public TimeSpan RemainingTime
        {
            get {
                if (!Elapsed)
                    return _timeOut - ElapsedTime;
                else
                    return TimeSpan.Zero;
            }
        }

        public TimeSpan ElapsedTime
        {
            get
            {
               return DateTime.Now - _timeStart;
            }
        }

        public bool Elapsed
		{
			get { return ElapsedTime >= _timeOut; }
		}

		public DownTimer()
		{
            _timeStart = DateTime.Now;
        }

		public DownTimer(TimeSpan timeOut)
		{
			_timeOut = timeOut;
            _timeStart = DateTime.Now;

        }

        public void Restart()
        {
            _timeStart = DateTime.Now;
        }
        public void SetTimeout(TimeSpan timeOut)
        {
            _timeOut = timeOut;
        }

        private TimeSpan _timeOut;
        private DateTime _timeStart;


		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~DownTimer() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}