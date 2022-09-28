using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<t>
{
    private int countAll;
    
    private Queue<t> inactiveQueue;
    private Queue<t> activeQueue;
    private Dictionary<t, bool> objectStateDict; //false if inactive

    private Action<t> _actionOnGet;
    private Action<t> _actionOnRelease;
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

        for(int i = 0; i < size; i++){
            t element = createFunc();
            inactiveQueue.Enqueue(element);
            objectStateDict.Add(element, false);
        }

    }

    ~ObjectPool(){
        foreach (t element in objectStateDict.Keys){
            this._actionOnDestroy(element);   
        }
    }

    private t getOldestActiveElement(){
        t element;
        do{
            element = activeQueue.Dequeue();
        } while (!objectStateDict[element]);
        return element;
    }

    public t Get(){
        t element;
        if(this.numInactive() == 0){
            element = getOldestActiveElement();
            this._actionOnRelease(element);
        } else {
            element = inactiveQueue.Dequeue();
        }

        activeQueue.Enqueue(element);
        this._actionOnGet(element);
        objectStateDict[element] = true;
        return element;
    }

    public void Release(t element){
        if(!objectStateDict.ContainsKey(element)){
            //error, object not from pool
            throw new InvalidOperationException("Object is not part of pool");
        }
        if(objectStateDict[element] == false){
            //error, object already inactive
            throw new InvalidOperationException("Object is already inactive");
        }

        this._actionOnRelease(element);
        objectStateDict[element] = false;
        inactiveQueue.Enqueue(element);
    }

    int numInactive(){
        return inactiveQueue.Count;
    }
    int numTotal(){
        return countAll;
    }
}
