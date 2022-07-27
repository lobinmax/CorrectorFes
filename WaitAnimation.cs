using DevExpress.Utils.Animation;

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Корректор_ФЭС
{
    public class AmpereWaitAnimation : TransitionManager
    {
        public static async Task StartWaitingIndicator(Control ownerControl, string caption, string description, Action action)
        {
            using (new DisableControl(ownerControl))
            {
                await Task.Run(() =>
                {
                    var manager = new AmpereWaitAnimation();
                    ownerControl.Invoke(new Action(() =>
                    {
                        manager.StartWaitingIndicator(ownerControl, new WaitingIndicatorProperties()
                        {
                            Caption = caption,
                            Description = description,
                            ShowCaption = true,
                            ShowDescription = true,
                            AllowBackground = true
                        });
                    }));

                    action.Invoke();

                    manager.EndTransition();
                });
            }
        }

        public static async Task StartWaitingIndicator(Control ownerControl, IWaitingIndicatorProperties waitingIndicatorProperties, Action action)
        {
            using (new DisableControl(ownerControl))
            {
                await Task.Run(() =>
                {
                    var manager = new AmpereWaitAnimation();
                    ownerControl.Invoke(new Action(() =>
                    {
                        manager.StartWaitingIndicator(ownerControl, waitingIndicatorProperties);
                    }));

                    action.Invoke();

                    manager.EndTransition();
                });
            }
        }

        public static async Task StartWaitingIndicator(Control ownerControl, Action action)
        {
            if (!ownerControl.IsHandleCreated)
            {
                return;
            }

            using (new DisableControl(ownerControl))
            {
                await Task.Run(() =>
                {
                    var manager = new AmpereWaitAnimation();
                    ownerControl.Invoke(new Action(() =>
                    {
                        manager.StartWaitingIndicator(ownerControl, new WaitingIndicatorProperties()
                        {
                            ShowCaption = false,
                            ShowDescription = false,
                            AllowBackground = false
                        });
                    }));

                    action.Invoke();

                    manager.EndTransition();
                });
            }
        }

        public static async Task StartWaitingIndicator(Control ownerControl, WaitingAnimatorType animatorType, Action action)
        {
            using (new DisableControl(ownerControl))
            {
                await Task.Run(() =>
                {
                    var manager = new AmpereWaitAnimation();
                    ownerControl.Invoke(new Action(() =>
                    {
                        manager.StartWaitingIndicator(ownerControl, animatorType);
                    }));

                    action.Invoke();

                    manager.EndTransition();
                });
            }
        }

        #region IDisposable
        private bool mDisposed;

        // реализация интерфейса IDisposable.
        public new void Dispose()
        {
            Dispose(true);
            // подавляем финализацию
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mDisposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                    this.EndTransition();
                }
                // освобождаем неуправляемые объекты
                mDisposed = true;
            }
        }

        ~AmpereWaitAnimation()
        {
            Dispose(false);
        }
        #endregion
    }

    public class DisableControl : IDisposable
    {
        private readonly Control mControl;
        public DisableControl(Control control)
        {
            mControl = control;

            if (mControl.InvokeRequired)
            {
                mControl.Invoke(new Action(() =>
                {
                    mControl.Enabled = false;
                }));
            }
            mControl.Enabled = false;
        }


        #region IDisposable
        private bool mDisposed;

        // реализация интерфейса IDisposable.
        public void Dispose()
        {
            Dispose(true);
            // подавляем финализацию
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mDisposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                    if (mControl.InvokeRequired)
                    {
                        mControl.Invoke(new Action(() =>
                        {
                            mControl.Enabled = true;
                        }));
                    }
                    mControl.Enabled = true;
                }
                // освобождаем неуправляемые объекты
                mDisposed = true;
            }
        }

        ~DisableControl()
        {
            Dispose(false);
        }
        #endregion

    }
}
