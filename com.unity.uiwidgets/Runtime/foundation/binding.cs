﻿using System;
using RSG;
using Unity.UIWidgets.async2;
using Unity.UIWidgets.ui2;

namespace Unity.UIWidgets.foundation {
    public abstract class BindingBase {
        protected BindingBase() {
            D.assert(!_debugInitialized);
            initInstances();
            D.assert(_debugInitialized);
        }

        static bool _debugInitialized = false;

        public Window window => Window.instance;

        protected virtual void initInstances() {
            D.assert(!_debugInitialized);
            D.assert(() => {
                _debugInitialized = true;
                return true;
            });
        }

        protected bool locked => _lockCount > 0;
        int _lockCount = 0;

        protected Future lockEvents(Func<Future> callback) {
            developer.Timeline.startSync("Lock events");

            D.assert(callback != null);
            _lockCount += 1;
            Future future = callback();
            D.assert(future != null,
                () =>
                    "The lockEvents() callback returned null; " +
                    "it should return a Promise that completes when the lock is to expire.");
            future.whenComplete(() => {
                _lockCount -= 1;
                if (!locked) {
                    developer.Timeline.finishSync();
                    unlocked();
                }

                return null;
            });
            return future;
        }

        protected virtual void unlocked() {
            D.assert(!locked);
        }
    }
}