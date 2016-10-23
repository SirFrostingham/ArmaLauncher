using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SteamLib
{
    public class QueryThreadPool
    {
        // The structure that this class uses internally to store queued callbacks and states.

        private struct InternalQueueItem
        {
            public System.Threading.WaitCallback callback;

            public object state;
            public InternalQueueItem(System.Threading.WaitCallback callback, object state)
            {
                this.callback = callback;
                this.state = state;
            }
        }

        // This is our queue for the query callbacks.
        private Queue<InternalQueueItem> queryQueue;
        // This is our list of ongoing queries.
        private List<System.ComponentModel.BackgroundWorker> queryThreads;
        // This variable tells us how many simultaneous queries are allowed
        private int simultaneousQueries;

        public event AllQueriesProcessedEventHandler AllQueriesProcessed;
        public delegate void AllQueriesProcessedEventHandler(object sender, EventArgs e);

        /// <summary>
        /// Creates a new QueryThreadPool instance.
        /// </summary>
        /// <param name="simultaneousQueries">The total amount of simultaneous queries.</param>
        /// <remarks></remarks>
        public QueryThreadPool(int simultaneousQueries)
        {
            this.simultaneousQueries = simultaneousQueries;
            this.queryThreads = new List<System.ComponentModel.BackgroundWorker>(this.simultaneousQueries);
            this.queryQueue = new Queue<InternalQueueItem>();
        }

        /// <summary>
        /// Adds a query to the queue. The query will be processed as soon
        /// as there are threads available.
        /// </summary>
        /// <param name="callback">The callback to the query</param>
        /// <param name="state">The arguments to be passed to the callback</param>
        /// <remarks></remarks>
        public void AddQuery(System.Threading.WaitCallback callback, object state)
        {
            try
            {
                this.queryQueue.Enqueue(new InternalQueueItem(callback, state));
                this.checkFreeThreads();
            }
            catch (Exception e)
            {
                GlobalsLib.Current.Logger.Error(e);
            }
        }

        // This method will check if there are any free threads for querying.
        private void checkFreeThreads()
        {
            if (this.queryQueue.Count > 0)
            {
                if (this.queryThreads.Count < this.simultaneousQueries)
                {
                    InternalQueueItem nextItem = this.queryQueue.Dequeue();
                    System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();
                    worker.DoWork += this.BackgroundWorker_DoWork;
                    worker.RunWorkerCompleted += this.BackgroundWorker_WorkCompleted;
                    worker.RunWorkerAsync(nextItem);

                    //do this check again before adding another thread
                    if (queryThreads.Count < simultaneousQueries)
                        queryThreads.Add(worker);
                }
            }
            else
            {
                if (AllQueriesProcessed != null)
                {
                    AllQueriesProcessed(this, EventArgs.Empty);
                }
            }

        }

        // This method cancels all pending queries.
        public void CancelAll()
        {
            try
            {
                //We cant do much about the workers that we have already started.
                //But we can clear the queryQueue to make sure no more gets started.
                this.queryQueue.Clear();
                if (AllQueriesProcessed != null)
                {
                    AllQueriesProcessed(this, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                GlobalsLib.Current.Logger.Error(e);
            }
        }

        // This is our internal DoWork handler.
        private void BackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            InternalQueueItem workItem = (InternalQueueItem)e.Argument;
            if (workItem.callback != null)
            {
                workItem.callback(workItem.state);
            }
        }

        // This is our internal WorkCompleted handler. It will be invoked when a query is completed.
        private void BackgroundWorker_WorkCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            try
            {
                this.queryThreads.Remove((System.ComponentModel.BackgroundWorker) sender);
                this.checkFreeThreads();
            }
            catch (Exception exception)
            {
                Debug.Write(exception);
            }
        }


    }
}


