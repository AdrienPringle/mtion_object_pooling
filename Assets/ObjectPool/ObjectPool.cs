using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<t>
{
    private int countAll;
    
    //fifo queue that stores inactive elements
    private Queue<t> inactiveQueue;

    //fifo queue that stores active elements
    private Queue<t> activeQueue;

    //hashmap from element to active state. element mapped to false if inactive
    private Dictionary<t, bool> objectStateDict;

    //callback when getting new element
    private Action<t> _actionOnGet;

    //callback when releasing element
    private Action<t> _actionOnRelease;

    //callback to clean up on destructor call 
    private Action<t> _actionOnDestroy;

    public ObjectPool(
        Func<t> createFunc,
        Action<t> actionOnGet,
        Action<t> actionOnRelease,
        Action<t> actionOnDestroy,
        int size
    ){
        this.countAll = size;
        this._actionOnDestroy = actionOnDestroy;
        this._actionOnGet = actionOnGet;
        this._actionOnRelease = actionOnRelease;

        inactiveQueue = new Queue<t>();
        activeQueue = new Queue<t>();
        objectStateDict = new Dictionary<t, bool>();
        
        //initialize full inactive queue, and states
        for(int i = 0; i < size; i++){
            t element = createFunc();
            inactiveQueue.Enqueue(element);
            objectStateDict.Add(element, false);
        }

    }

    // Destroy all active and inactive elements
    // The keys of the element hash map provide a list of all existing elements
    ~ObjectPool(){
        foreach (t element in objectStateDict.Keys){
            this._actionOnDestroy(element);   
        }
    }

    private t getOldestActiveElement(){
        // The active queue is not pruned when elements are released
        // therefore, we need to loop through all inactive elements
        // until an active element is found
        t element;
        do{
            element = activeQueue.Dequeue();
        } while (!objectStateDict[element]);
        return element;
    }

    // Return a new element from pool
    public t Get(){
        t element;

        // reuse an active element if no inactive elements exist
        if(this.numInactive() == 0){
            element = getOldestActiveElement();
            this._actionOnRelease(element);
        } else {
            element = inactiveQueue.Dequeue();
        }

        // update element state and active queue
        activeQueue.Enqueue(element);
        objectStateDict[element] = true;

        // callback on element get
        this._actionOnGet(element);
        
        return element;
    }

    // Release an active element back into pool
    public void Release(t element){
        if(!objectStateDict.ContainsKey(element)){
            //error, object not from pool
            throw new InvalidOperationException("Object is not part of pool");
        }
        if(objectStateDict[element] == false){
            //error, object already inactive
            throw new InvalidOperationException("Object is already inactive");
        }


        // update element state and active queue
        objectStateDict[element] = false;
        inactiveQueue.Enqueue(element);

        // callback on element release
        this._actionOnRelease(element);
    }

    int numInactive(){
        return inactiveQueue.Count;
    }
    int numTotal(){
        return countAll;
    }
}
