using UnityEditor;
using UnityEngine;

public class GameEvent : MonoBehaviour
{
    bool alreadyEventSend ;
    public void GameEventCatcher()
    {
        int playerClickCount = 0;
        string movementMessage;
        float decisiveFactorCoefficient;
        bool playerTouched = false;
        while (true)
        {
            if(playerTouched) playerClickCount += 1;
            if(playerClickCount > 5)
            {
                // register this player response and update the file accordingly 
                playerTouched = true;
                break;
            }
        }


        if (playerTouched) {
            // invoke and outside word event that informs user that user has touched the screen 
            SendEvent();

        }

    }
    //
    // Summary: 
    //          THis function sends and touch event from mobie to web and return and turth value as bollean engadment
    public bool SendEvent()
    {
        Debug.Log("Evennt is being send to the Panel");
        if (alreadyEventSend != true) { 
           alreadyEventSend = true;
            // send the function whihc will thorw the outside event fromt his function
            return true;
        }
        // if we hav e already send event with in some mili sec this event will not be send again atleast 12 mili sec gap for this each action
        // why 12 sec as normal human to register a event the gap should be between 12 and 22
        return false;
    }
}
