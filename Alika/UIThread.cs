using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Alika.UI
{
    public class UITasksLoop
    {
        private List<UITask> Actions = new List<UITask>();

        public UITasksLoop()
        {
            //Task.Run(() => this.Loop());
        }

        public void AddAction(UITask task) => this.RunAction(task);//this.Actions.Add(task); // For future maybe

        public async void RunAction(UITask task) => await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(task.Priority, task.Action);

        /*public async void Loop()
        {
            while (true)
            {
                if (this.Actions.Count > 0)
                {
                    var sortedActions = Actions.OrderByDescending(x => (int)x.Priority).ToList();
                    for (int x = 0; x < sortedActions.Count; x++)
                    {
                        System.Diagnostics.Debug.WriteLine("\n[UILOOP] [Loop] Getting task");
                        var task = sortedActions[x];
                        System.Diagnostics.Debug.WriteLine("[UILOOP] [Loop] Sorting actions");
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(task.Priority, task.Action);
                        System.Diagnostics.Debug.WriteLine("[UILOOP] [Loop] Executed, removing from list");
                        Actions.Remove(task);
                        System.Diagnostics.Debug.WriteLine("[UILOOP] [Loop] Removed\n");
                    }
                }
            }
        }*/
    }

    public class UITask
    {
        public DispatchedHandler Action { get; set; }
        public CoreDispatcherPriority Priority { get; set; } = CoreDispatcherPriority.Normal;
    }
}
