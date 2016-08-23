using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class VoxBox : MonoBehaviour {

    static private Queue<object> messageQueue;
    static private object currentMessage;
    public delegate void voidHandler();

    // -- Typical Flow --
    // onEnter
    // onBuildWindow(rect position)
    // onTextUpdate  <---\
    // onAwaitInput -----/
    // onWindowTeardown(bool immediate?)
    // onExit

    // Occurs when Dialoguer starts a dialogue.
    static public event voidHandler onEnter;

    // Occurs when VoxBox enters a TextPhase in a dialogue.
    // Use this to build the window. Everything to display your text should be initialized by end of this.
    static public event BuildWindowHandler onBuildWindow;
    public delegate IEnumerator BuildWindowHandler(object data);

    // Occurs when VoxBox advances to the next message.
    // Covert the data to your class, and use its data as you see fit.
    static public event TextUpdateHandler onTextUpdate;
    public delegate void TextUpdateHandler(object data);

    // Occurs when VoxBox calls for the text window to close.
    // This is skipped if the message is 'continual' and should flow into the next.
    static public event WindowTeardownHandler onWindowTeardown;
    public delegate IEnumerator WindowTeardownHandler(bool closeInstantly=false);

    // Occurs when VoxBox ends a dialogue.
    static public event voidHandler onExit;

    // Called at completeion of entire dialoug.
    static public Callback onComplete;

    // Set to true to not call teardown/exit. Used to chain multiple different messages into one continuous dialouge.
    // Sets to true at beginning of onTextUpdate. You should updated with hooks.
    static public bool shouldTearDownWindow = true;

    //Singleton
    static public VoxBox _this;
    static Coroutine co;


    void Awake() {
        _this = this;
        messageQueue = new Queue<object>();
    }

    static public void QueueMessage(object message) {
        messageQueue.Enqueue(message);
    }


    static private object NextMessage() {
        currentMessage = messageQueue.Dequeue();
        return currentMessage;
    }

    static public void PlayMessages(Callback callback = null) {
        // If instance alreay running, kill it first.
        if (co != null)
            ForceEndMessages();
        co = null;
        if (messageQueue.Count == 0)
            return;
        onComplete = callback;
        if (onEnter != null && shouldTearDownWindow)
            onEnter();
        co = _this.StartCoroutine(RunMessages());
    }

    static IEnumerator RunMessages() {
        currentMessage = NextMessage();
        if (onBuildWindow != null && shouldTearDownWindow)
            yield return onBuildWindow(currentMessage);
        OnTextUpdate(currentMessage);
    }

    static public void ContinueMessages() {
        if (messageQueue.Count > 0)
        {
            currentMessage = NextMessage();
            OnTextUpdate(currentMessage);
        }
        else
            EndMessages();
    }

    static private void OnTextUpdate(object currentMessage) {
        shouldTearDownWindow = true;
        if (onTextUpdate != null)
            onTextUpdate(currentMessage);
    }


    // Ends messages with dirty flag = True
    static public void ForceEndMessages() {
        _this.StopCoroutine(co);
        _this.StartCoroutine(EndingMessages(true));
    }

    // Ends messages
    static public void EndMessages() {
        currentMessage = null;
        co = _this.StartCoroutine(EndingMessages(false));
    }

    static IEnumerator EndingMessages(bool dirtyClose) {
        if (shouldTearDownWindow || dirtyClose)
        {
            if (onWindowTeardown != null)
                yield return onWindowTeardown(dirtyClose);
        }
        if (onExit != null)
            onExit();
        //Callback
        co = null;
        if (onComplete != null)
            onComplete();
    }


}
