using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Experiments : MonoBehaviour
{
    int LargestSubString(string str, int k)
    {
        Dictionary<char, int> tracker = new Dictionary<char, int>();
        int left = 0;
        int longest = 0;

        for (int right = 0; right < str.Length; right++)
        {
            int localLongest = 0;
            if(tracker.ContainsKey(str[right]))
                    tracker[str[right]]++;
            else
                tracker.Add(str[right], 1);

            
                while (tracker[str[right]] > k)
                {
                    tracker[str[left]]--;
                    if(tracker[str[left]] == 0)
                        tracker.Remove(str[left]);
                    left++;
                }
                
                
            
            
                localLongest = right - left + 1;

            longest = Mathf.Max(longest, localLongest);
        }

        return longest;
    }
}