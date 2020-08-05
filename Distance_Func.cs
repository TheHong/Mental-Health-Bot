//https://nickgrattan.wordpress.com/2014/06/10/euclidean-manhattan-and-cosine-distance-measures-in-c/ //old
//https://nickgrattandatascience.wordpress.com/2017/12/31/euclidean-manhattan-and-cosine-distance-measures-in-c/
using System;
using System.Collections.Generic;
using System.Linq;
namespace Distance_Func{

        /// <summary>
    /// Manages Frequency Distributions for items of type T
    /// </summary>
    /// <typeparam name="T">Type for item</typeparam>
    public class FrequencyDist<T>
    {
        /// <summary>
        /// Construct Frequency Distribution for the given list of items
        /// </summary>
        /// <param name="li">List of items to calculate for</param>
        public FrequencyDist(List<T> li)
        {
            CalcFreqDist(li);
        }
    
        /// <summary>
        /// Construct Frequency Distribution for the given list of items, across all keys in itemValues
        /// </summary>
        /// <param name="li">List of items to calculate for</param>
        /// <param name="itemValues">Entire list of itemValues to include in the frequency distribution</param>
        public FrequencyDist(List<T> li, List<T> itemValues)
        {
            CalcFreqDist(li);
            // add items to frequency distribution that are in itemValues but missing from the frequency distribution
            foreach (var v in itemValues)
            {
                if(!ItemFreq.Keys.Contains(v))
                {
                    ItemFreq.Add(v, new Item { value = v, count = 0 });
                }
            }
            // check that all values in li are in the itemValues list
            foreach(var v in li)
            {
                if (!itemValues.Contains(v))
                    throw new Exception(string.Format("FrequencyDist: Value in list for frequency distribution not in supplied list of values: '{0}'.", v));
            }
        }
    
        /// <summary>
        /// Calculate the frequency distribution for the values in list
        /// </summary>
        /// <param name="li">List of items to calculate for</param>
        void CalcFreqDist(List<T> li)
        {
            itemFreq = new SortedList<T,Item>((from item in li
                group item by item into theGroup
                select new Item { value = theGroup.FirstOrDefault(), count = theGroup.Count() }).ToDictionary(q => q.value, q => q));
        }
        SortedList<T, Item> itemFreq = new SortedList<T, Item>();
    
        /// <summary>
        /// Getter for the Item Frequency list
        /// </summary>
        public SortedList<T, Item> ItemFreq { get { return itemFreq; } }
    
        public int Freq(T value)
        {
            if(itemFreq.Keys.Contains(value))
            {
                return itemFreq[value].count;
            }
            else
            {
                return 0;
            }
        }
    
        /// <summary>
        /// Returns the list of distinct values between two lists
        /// </summary>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <returns></returns>
        public static List<T> GetDistinctValues(List<T> l1, List<T> l2)
        {
            return l1.Concat(l2).ToList().Distinct().ToList();
        }
    
        /// <summary>
        /// Manages a count of items (int, string etc) for frequency counts
        /// </summary>
        /// <typeparam name="T">The type for item</typeparam>
        public class Item
        {
            /// <summary>
            /// The value of the item, e.g. int or string
            /// </summary>
            public T value { get; set; }
            /// <summary>
            /// The count of the item
            /// </summary>
            public int count { get; set; }
        }
    }
    public class LrNorm
    {
        /// <summary>
        /// Returns Euclidean distance between frequency distributions of two lists
        /// </summary>
        /// <typeparam name="T">Type of the item, e.g. int or string</typeparam>
        /// <param name="l1">First list of items</param>
        /// <param name="l2">Second list of items</param>
        /// <returns>Distance, 0 - identical</returns>
        public static double Euclidean<T>(List<T> l1, List<T> l2)
        {
            return DoLrNorm(l1, l2, 2);
        }
    
        /// <summary>
        /// Returns Manhattan distance between frequency distributions of two lists
        /// </summary>
        /// <typeparam name="T">Type of the item, e.g. int or string</typeparam>
        /// <param name="l1">First list of items</param>
        /// <param name="l2">Second list of items</param>
        /// <returns>Distance, 0 - identical</returns>
        public static double Manhattan<T>(List<T> l1, List<T> l2)
        {
            return DoLrNorm(l1, l2, 1);
        }
    
        /// <summary>
        /// Returns LrNorm distance between frequency distributions of two lists
        /// </summary>
        /// <typeparam name="T">Type of the item, e.g. int or string</typeparam>
        /// <param name="l1">First list of items</param>
        /// <param name="l2">Second list of items</param>
        /// <param name="r">Power to use 2 = Euclidean, 1 = Manhattan</param>
        /// <returns>Distance, 0 - identical</returns>
        public static double DoLrNorm<T>(List<T> l1, List<T> l2, int r)
        {
            // find distinct list of values from both lists.
            List<T> dvs = FrequencyDist<T>.GetDistinctValues(l1, l2);
    
            // create frequency distributions aligned to list of descrete values
            FrequencyDist<T> fd1 = new FrequencyDist<T>(l1, dvs);
            FrequencyDist<T> fd2 = new FrequencyDist<T>(l2, dvs);
    
            if (fd1.ItemFreq.Count != fd2.ItemFreq.Count)
            {
                throw new Exception("Lists of different length for LrNorm calculation");
            }
            double sumsq = 0.0;
            for (int i = 0; i < fd1.ItemFreq.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(fd1.ItemFreq.Values[i].value, fd2.ItemFreq.Values[i].value))
                    throw new Exception("Mismatched values in frequency distribution for LrNorm calculation");
    
                if (r == 1)   // Manhattan optimization
                {
                    sumsq += Math.Abs((fd1.ItemFreq.Values[i].count - fd2.ItemFreq.Values[i].count));
                }
                else
                {
                    sumsq += Math.Pow((double)Math.Abs((fd1.ItemFreq.Values[i].count - fd2.ItemFreq.Values[i].count)), r);
                }
            }
            if (r == 1)    // Manhattan optimization
            {
                return sumsq;
            }
            else
            {
                return Math.Pow(sumsq, 1.0 / r);
            }
        }
    }


/// Calculate cosine distance between two vectors
/// </summary>
    public class Cosine
    {
        /// <summary>
        /// Calculates the distance between frequency distributions calculated from lists of items
        /// </summary>
        /// <typeparam name="T">Type of the list item, e.g. int or string</typeparam>
        /// <param name="l1">First list of items</param>
        /// <param name="l2">Second list of items</param>
        /// <returns>Distance in degrees. 90 is totally different, 0 exactly the same</returns>
        public static double Distance<T>(List<T> l1, List<T> l2)
        {
            if (l1.Count == 0 || l2.Count == 0)
            {
                throw new Exception("Cosine Distance: lists cannot be zero length");
            }
    
            // find distinct list of items from two lists, used to align frequency distributions from two lists
            List<T> dvs = FrequencyDist<T>.GetDistinctValues(l1, l2);
            // calculate frequency distributions for each list.
            FrequencyDist<T> fd1 = new FrequencyDist<T>(l1, dvs);
            FrequencyDist<T> fd2 = new FrequencyDist<T>(l2, dvs);
    
            if(fd1.ItemFreq.Count != fd2.ItemFreq.Count)
            {
                throw new Exception("Cosine Distance: Frequency count vectors must be same length");
            }
            double dotProduct = 0.0;
            double l2norm1 = 0.0;
            double l2norm2 = 0.0;
            for(int i = 0; i < fd1.ItemFreq.Values.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(fd1.ItemFreq.Values[i].value, fd2.ItemFreq.Values[i].value))
                    throw new Exception("Mismatched values in frequency distribution for Cosine distance calculation");
    
                dotProduct += fd1.ItemFreq.Values[i].count * fd2.ItemFreq.Values[i].count;
                l2norm1 += fd1.ItemFreq.Values[i].count * fd1.ItemFreq.Values[i].count;
                l2norm2 += fd2.ItemFreq.Values[i].count * fd2.ItemFreq.Values[i].count;
            }
            double cos = dotProduct / (Math.Sqrt(l2norm1) * Math.Sqrt(l2norm2));
            // convert cosine value to radians then to degrees
            return Math.Acos(cos) * 180.0 / Math.PI;
        }
    }
}        