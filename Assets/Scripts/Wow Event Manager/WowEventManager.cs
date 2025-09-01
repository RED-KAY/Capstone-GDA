using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace Wow.Events
{
    public class WowEventManager : SingletonPersistent<WowEventManager>
    {
        private static Dictionary<string, List<WowInvokableActionBase>> globalEventTable = new Dictionary<string, List<WowInvokableActionBase>>();

        protected virtual void Init()
        {
            if (globalEventTable == null)
            {
                globalEventTable = new Dictionary<string, List<WowInvokableActionBase>>();
            }
        }

        private static List<WowInvokableActionBase> GetActionList(string eventName)
        {
            if (globalEventTable.TryGetValue(eventName, out var value))
            {
                return value;
            }
            return null;
        }

        private static void CheckForEventRemoval(string eventName, List<WowInvokableActionBase> actionList)
        {
            if (actionList.Count == 0)
            {
                globalEventTable.Remove(eventName);
            }
        }
        
        /// <summary>
        /// A static interface to subscribe to a default Action
        /// </summary>
        /// <param name="eventName">The name of the Action to subscribe</param>
        /// <param name="action">The Action to be executed on intercepting the Action</param>
        public static void StartListening(string eventName, Action action)
        {
            WowInvokableAction invokableAction = new WowInvokableAction();
            invokableAction.Initialize(action);
            RegisterEvent(eventName, invokableAction);
        }
        
        /// <summary>
        /// A static interface to subscribe to a single parameter Action
        /// </summary>
        /// <param name="eventName">The name of the Action to subscribe</param>
        /// <param name="action">The Action to be executed on intercepting the Action</param>
        public static void StartListening<T1>(string eventName, Action<T1> action)
        {
            WowInvokableAction<T1> invokableAction = new WowInvokableAction<T1>();
            invokableAction.Initialize(action);
            RegisterEvent(eventName, invokableAction);
        }
        
        /// <summary>
        /// A static interface to subscribe to two parameters Action
        /// </summary>
        /// <param name="eventName">The name of the Action to subscribe</param>
        /// <param name="action">The Action to be executed on intercepting the Action</param>
        public static void StartListening<T1, T2>(string eventName, Action<T1, T2> action)
        {
            WowInvokableAction<T1, T2> invokableAction = new WowInvokableAction<T1, T2>();
            invokableAction.Initialize(action);
            RegisterEvent(eventName, invokableAction);
        }
        
        /// <summary>
        /// A static interface to subscribe to three parameters Action
        /// </summary>
        /// <param name="eventName">The name of the Action to subscribe</param>
        /// <param name="action">The Action to be executed on intercepting the Action</param>
        public static void StartListening<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
        {
            WowInvokableAction<T1, T2, T3> invokableAction = new WowInvokableAction<T1, T2, T3>();
            invokableAction.Initialize(action);
            RegisterEvent(eventName, invokableAction);
        }

        private static void RegisterEvent(string eventName, WowInvokableActionBase listener)
        {
            if (globalEventTable.TryGetValue(eventName, out var thisEvent))
            {
                thisEvent.Add(listener);
            }
            else
            {
                thisEvent = new List<WowInvokableActionBase>();
                thisEvent.Add(listener);
                globalEventTable.Add(eventName, thisEvent);
            }
        }

        /// <summary>
        /// A static interface to unsubscribe to a default UnityEvent
        /// </summary>
        /// <param name="eventName">The name of the UnityEvent to unsubsubscribe from</param>
        /// <param name="listener">The listener that needs to be removed</param>
        public static void StopListening(string eventName, Action listener)
        {
            List<WowInvokableActionBase> actionList = GetActionList(eventName);
            if (actionList == null)
            {
                return;
            }
            for (int i = 0; i < actionList.Count; i++)
            {
                WowInvokableAction invokableAction = actionList[i] as WowInvokableAction;
                if (invokableAction.IsAction(listener))
                {
                    //GSGenericObjectPool.Return<GSInvokableAction>(invokableAction);
                    actionList.RemoveAt(i);
                    break;
                }
            }
            CheckForEventRemoval(eventName, actionList);
        }

        public static void StopListening<T1>(string eventName, Action<T1> action)
        {
            List<WowInvokableActionBase> actionList = GetActionList(eventName);
            if (actionList == null)
            {
                return;
            }
            for (int i = 0; i < actionList.Count; i++)
            {
                WowInvokableAction<T1> invokableAction = actionList[i] as WowInvokableAction<T1>;
                if (invokableAction.IsAction(action))
                {
                    //GSGenericObjectPool.Return<GSInvokableAction<T1>>(invokableAction);
                    actionList.RemoveAt(i);
                    break;
                }
            }
            CheckForEventRemoval(eventName, actionList);
        }
        
        public static void StopListening<T1, T2>(string eventName, Action<T1, T2> action)
        {
            List<WowInvokableActionBase> actionList = GetActionList(eventName);
            if (actionList == null)
            {
                return;
            }
            for (int i = 0; i < actionList.Count; i++)
            {
                WowInvokableAction<T1, T2> invokableAction = actionList[i] as WowInvokableAction<T1, T2>;
                if (invokableAction.IsAction(action))
                {
                    //GSGenericObjectPool.Return<GSInvokableAction<T1>>(invokableAction);
                    actionList.RemoveAt(i);
                    break;
                }
            }
            CheckForEventRemoval(eventName, actionList);
        }
        
        public static void StopListening<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
        {
            List<WowInvokableActionBase> actionList = GetActionList(eventName);
            if (actionList == null)
            {
                return;
            }
            for (int i = 0; i < actionList.Count; i++)
            {
                WowInvokableAction<T1, T2, T3> invokableAction = actionList[i] as WowInvokableAction<T1, T2, T3>;
                if (invokableAction.IsAction(action))
                {
                    //GSGenericObjectPool.Return<GSInvokableAction<T1>>(invokableAction);
                    actionList.RemoveAt(i);
                    break;
                }
            }
            CheckForEventRemoval(eventName, actionList);
        }

        /// <summary>
        /// A static interface to trigger a UnityEvent
        /// </summary>
        /// <param name="eventName">The name of the UnityEvent that needs to be triggered</param>
        public static void TriggerEvent(string eventName)
        {
            List<WowInvokableActionBase> actionList = GetActionList(eventName);
            if (actionList != null)
            {
                for (int num = actionList.Count - 1; num >= 0; num--)
                {
                    (actionList[num] as WowInvokableAction).Invoke();
                }
            }
        }

        /// <summary>
        /// A static interface to trigger a UnityEvent having single string parameter
        /// </summary>
        /// <param name="eventName">The name of the UnityEvent that needs to be triggered</param>
        /// <param name="param">The object of T type that is to be passed to the listener</param>
        public static void TriggerEvent<T1>(string eventName, T1 param)
        {
            List<WowInvokableActionBase> actionList = GetActionList(eventName);
            if (actionList != null)
            {
                for (int num = actionList.Count - 1; num >= 0; num--)
                {
                    (actionList[num] as WowInvokableAction<T1>).Invoke(param);
                }
            }
        }
        
        public static void TriggerEvent<T1, T2>(string eventName, T1 param, T2 param2)
        {
            List<WowInvokableActionBase> actionList = GetActionList(eventName);
            if (actionList != null)
            {
                for (int num = actionList.Count - 1; num >= 0; num--)
                {
                    (actionList[num] as WowInvokableAction<T1, T2>).Invoke(param, param2);
                }
            }
        }
        
        public static void TriggerEvent<T1, T2, T3>(string eventName, T1 param, T2 param2, T3 param3)
        {
            List<WowInvokableActionBase> actionList = GetActionList(eventName);
            if (actionList != null)
            {
                for (int num = actionList.Count - 1; num >= 0; num--)
                {
                    (actionList[num] as WowInvokableAction<T1, T2, T3>).Invoke(param, param2, param3);
                }
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void DomainReset()
        {
            if (globalEventTable != null)
            {
                globalEventTable.Clear();
            }
        }
    }
}

