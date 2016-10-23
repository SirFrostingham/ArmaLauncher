using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace ArmaLauncher.Models
{
    /// <summary>
    /// Demo implementation of IItemsProvider returning dummy customer items after
    /// a pause to simulate network/disk latency.
    /// </summary>
    public class ArmaServerProvider : IItemsProvider<server>
    {
        private readonly int _count;
        private readonly int _fetchDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArmaServerProvider"/> class.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="fetchDelay">The fetch delay.</param>
        public ArmaServerProvider(int count, int fetchDelay)
        {
            _count = count;
            _fetchDelay = fetchDelay;
        }

        /// <summary>
        /// Fetches the total number of items available.
        /// </summary>
        /// <returns></returns>
        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            Thread.Sleep(_fetchDelay);
            return _count;
        }

        /// <summary>
        /// Fetches a range of items.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The number of items to fetch.</param>
        /// <returns></returns>
        public IList<server> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("FetchRange: " + startIndex + "," + count);
            Thread.Sleep(_fetchDelay);

            var list = new List<server>();
            for (int i = startIndex; i < startIndex + count; i++)
            {
                var server = Globals.Current.ArmaServers[i];
                list.Add(server);
            }
            return list;
        }
    }
}
