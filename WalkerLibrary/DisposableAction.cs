using System;

namespace WalkerLibrary
{
    public class DisposableAction : IDisposable
    {
        private Action _dispose;

        public DisposableAction(Action dispose)
        {
            if (dispose == null) throw new ArgumentNullException("dispose");
            this._dispose = dispose;
        }

        public DisposableAction(Action construct, Action dispose)
        {
            if (construct == null) throw new ArgumentNullException("construct");
            if (dispose == null) throw new ArgumentNullException("dispose");

            construct();
            this._dispose = dispose;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._dispose == null)
                    return;

                try { this._dispose(); }
                catch (Exception ex) { /* ToDo: Log error? */ }

                this._dispose = null;
            }
        }
    }
}