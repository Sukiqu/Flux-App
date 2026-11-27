using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Akis
{
    // Track nested HWNDs as one interaction surface. No polling or animation
    // timer: invalidation occurs only when the interaction state changes.
    internal sealed class HoverTracker : IDisposable
    {
        private readonly Control owner;
        private readonly HashSet<Control> watched=new HashSet<Control>();
        private readonly HashSet<Control> inside=new HashSet<Control>();
        internal bool Active {get;private set;}
        internal HoverTracker(Control owner){this.owner=owner;Attach(owner);owner.Disposed+=Disposed;}
        private void Attach(Control c)
        {
            if(!watched.Add(c))return;
            c.MouseEnter+=Enter;c.MouseLeave+=Leave;c.GotFocus+=FocusChanged;c.LostFocus+=FocusChanged;
            c.VisibleChanged+=AvailabilityChanged;c.EnabledChanged+=AvailabilityChanged;
            c.ControlAdded+=Added;c.ControlRemoved+=Removed;
            foreach(Control child in c.Controls)Attach(child);
        }
        private void Detach(Control c)
        {
            if(!watched.Remove(c))return;
            inside.Remove(c);
            c.MouseEnter-=Enter;c.MouseLeave-=Leave;c.GotFocus-=FocusChanged;c.LostFocus-=FocusChanged;
            c.VisibleChanged-=AvailabilityChanged;c.EnabledChanged-=AvailabilityChanged;
            c.ControlAdded-=Added;c.ControlRemoved-=Removed;
            foreach(Control child in c.Controls)Detach(child);
        }
        private void Enter(object sender,EventArgs e){var c=(Control)sender;if(c.Enabled&&c.Visible)inside.Add(c);Update();}
        private void Leave(object sender,EventArgs e){inside.Remove((Control)sender);Update();}
        private void FocusChanged(object sender,EventArgs e){Update();}
        private void AvailabilityChanged(object sender,EventArgs e)
        {var c=(Control)sender;if(!c.Visible||!c.Enabled)inside.Remove(c);if(!owner.Visible||!owner.Enabled)inside.Clear();Update();}
        private void Added(object sender,ControlEventArgs e){Attach(e.Control);Update();}
        private void Removed(object sender,ControlEventArgs e){Detach(e.Control);Update();}
        private void Update()
        {
            bool next=owner.Visible&&owner.Enabled&&(inside.Count>0||owner.ContainsFocus);
            if(next==Active)return;Active=next;
            if(!owner.IsDisposed)owner.Invalidate(true);
        }
        private void Disposed(object sender,EventArgs e){Dispose();}
        public void Dispose()
        {
            owner.Disposed-=Disposed;
            foreach(var c in new List<Control>(watched))Detach(c);
            inside.Clear();Active=false;
        }
    }
}
