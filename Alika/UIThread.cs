using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;

namespace Alika.UI
{
    public class UITasksLoop
    {
        private List<UITask> Actions = new List<UITask>();

        public UITasksLoop()
        {
            Task.Factory.StartNew(() => this.Loop());
        }

        public void AddAction(UITask task) => this.Actions.Add(task); // For future maybe

        public async void RunAction(UITask task) => await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(task.Priority, task.Action);

        public async void Loop()
        {
            while (true)
            {
                if(this.Actions.Count > 0) {
                    var sortedActions = Actions.OrderByDescending(x => (int)x.Priority).ToList();
                    for(int x = 0; x < sortedActions.Count; x++)
                    {
                        var task = sortedActions[x];
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(task.Priority, task.Action);
                        Actions.Remove(task);
                    }
                }
            }
        }
    }

    public class UITask
    {
        public DispatchedHandler Action { get; set; }
        public CoreDispatcherPriority Priority { get; set; } = CoreDispatcherPriority.Normal;
    }
}
