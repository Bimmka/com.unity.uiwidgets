using System.Collections.Generic;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.scheduler;
using UnityEditor;

namespace Unity.UIWidgets.gestures {
#if UNITY_EDITOR
    public partial class MouseTracker {
        bool _enableDragFromEditorRelease = false;

        void _handleDragFromEditorEvent(PointerEvent evt, int deviceId) {
            if (!inEditorWindow) {
                return;
            }

            if (evt is PointerDragFromEditorReleaseEvent) {
                _enableDragFromEditorRelease = false;
                _scheduleDragFromEditorReleaseCheck();
                _lastMouseEvent.Remove(deviceId);
            }
            else if (evt is PointerDragFromEditorEnterEvent ||
                     evt is PointerDragFromEditorHoverEvent ||
                     evt is PointerDragFromEditorExitEvent) {
                if (!_lastMouseEvent.ContainsKey(deviceId) ||
                    _lastMouseEvent[deviceId].position != evt.position) {
                    _scheduleDragFromEditorMousePositionCheck();
                }

                _lastMouseEvent[deviceId] = evt;
            }
        }

        void detachDragFromEditorAnnotation(MouseTrackerAnnotation annotation, int deviceId) {
            if (!inEditorWindow) {
                return;
            }

            if (annotation.onDragFromEditorExit != null) {
                annotation.onDragFromEditorExit(
                    PointerDragFromEditorExitEvent.fromDragFromEditorEvent(_lastMouseEvent[deviceId]));
            }
        }

        void _scheduleDragFromEditorReleaseCheck() {
            DragAndDrop.AcceptDrag();

            var lastMouseEvent = new List<PointerEvent>();
            foreach (int deviceId in _lastMouseEvent.Keys) {
                var _deviceId = deviceId;
                lastMouseEvent.Add(_lastMouseEvent[_deviceId]);
                SchedulerBinding.instance.addPostFrameCallback(_ => {
                    foreach (var lastEvent in lastMouseEvent) {
                        MouseTrackerAnnotation hit = annotationFinder(lastEvent.position);

                        if (hit == null) {
                            foreach (_TrackedAnnotation trackedAnnotation in _trackedAnnotations.Values) {
                                if (trackedAnnotation.activeDevices.Contains(_deviceId)) {
                                    trackedAnnotation.activeDevices.Remove(_deviceId);
                                }
                            }

                            return;
                        }

                        _TrackedAnnotation hitAnnotation = _findAnnotation(hit);

                        // release
                        if (hitAnnotation.activeDevices.Contains(_deviceId)) {
                            if (hitAnnotation.annotation?.onDragFromEditorRelease != null) {
                                hitAnnotation.annotation.onDragFromEditorRelease(
                                    PointerDragFromEditorReleaseEvent
                                        .fromDragFromEditorEvent(
                                            lastEvent, DragAndDrop.objectReferences));
                            }

                            hitAnnotation.activeDevices.Remove(_deviceId);
                        }
                    }
                });
            }

            SchedulerBinding.instance.scheduleFrame();
        }

        /// <summary>
        /// Due to the [DragAndDrop] property, DragAndDrop.visualMode must be set to Copy
        /// after which editor window can trigger DragPerform event.
        /// And because visualMode will be set to None when every frame finished in IMGUI,
        /// here we start a scheduler to update VisualMode in every post frame.
        /// When [_enableDragFromEditorRelease] set to false, it will stop, vice versa.
        /// </summary>
        void _enableDragFromEditorReleaseVisualModeLoop() {
            if (_enableDragFromEditorRelease) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                SchedulerBinding.instance.addPostFrameCallback(_ => {
                    _enableDragFromEditorReleaseVisualModeLoop();
                });
                SchedulerBinding.instance.scheduleFrame();
            }
        }

        void _scheduleDragFromEditorMousePositionCheck() {
            if (!inEditorWindow) {
                return;
            }

            SchedulerBinding.instance.addPostFrameCallback(_ => { collectDragFromEditorMousePositions(); });
            SchedulerBinding.instance.scheduleFrame();
        }

        public void collectDragFromEditorMousePositions() {
            void exitAnnotation(_TrackedAnnotation trackedAnnotation, int deviceId) {
                if (trackedAnnotation.activeDevices.Contains(deviceId)) {
                    _enableDragFromEditorRelease = false;
                    if (trackedAnnotation.annotation?.onDragFromEditorExit != null) {
                        trackedAnnotation.annotation.onDragFromEditorExit(
                            PointerDragFromEditorExitEvent.fromDragFromEditorEvent(
                                _lastMouseEvent[deviceId]));
                    }

                    trackedAnnotation.activeDevices.Remove(deviceId);
                }
            }

            void exitAllDevices(_TrackedAnnotation trackedAnnotation) {
                if (trackedAnnotation.activeDevices.isNotEmpty()) {
                    HashSet<int> deviceIds = new HashSet<int>(trackedAnnotation.activeDevices);
                    foreach (int deviceId in deviceIds) {
                        exitAnnotation(trackedAnnotation, deviceId);
                    }
                }
            }

            if (!mouseIsConnected) {
                foreach (var annotation in _trackedAnnotations.Values) {
                    exitAllDevices(annotation);
                }

                return;
            }

            foreach (int deviceId in _lastMouseEvent.Keys) {
                PointerEvent lastEvent = _lastMouseEvent[deviceId];
                MouseTrackerAnnotation hit = annotationFinder(lastEvent.position);

                if (hit == null) {
                    foreach (_TrackedAnnotation trackedAnnotation in _trackedAnnotations.Values) {
                        exitAnnotation(trackedAnnotation, deviceId);
                    }

                    return;
                }

                _TrackedAnnotation hitAnnotation = _findAnnotation(hit);

                // While acrossing two areas, set the flag to true to prevent setting the Pointer Copy VisualMode to None
                bool enterFlag = false;

                // enter
                if (!hitAnnotation.activeDevices.Contains(deviceId)) {
                    hitAnnotation.activeDevices.Add(deviceId);
                    enterFlag = true;
                    // Both onRelease or onEnter event will enable Copy VisualMode
                    if (hitAnnotation.annotation?.onDragFromEditorRelease != null ||
                        hitAnnotation.annotation?.onDragFromEditorEnter != null) {
                        if (!_enableDragFromEditorRelease) {
                            _enableDragFromEditorRelease = true;
                            _enableDragFromEditorReleaseVisualModeLoop();
                        }

                        if (hitAnnotation.annotation?.onDragFromEditorEnter != null) {
                            hitAnnotation.annotation.onDragFromEditorEnter(
                                PointerDragFromEditorEnterEvent
                                    .fromDragFromEditorEvent(lastEvent));
                        }
                    }
                }

                // hover
                if (hitAnnotation.annotation?.onDragFromEditorHover != null) {
                    hitAnnotation.annotation.onDragFromEditorHover(
                        PointerDragFromEditorHoverEvent.fromDragFromEditorEvent(lastEvent));
                }

                // leave
                foreach (_TrackedAnnotation trackedAnnotation in _trackedAnnotations.Values) {
                    if (hitAnnotation == trackedAnnotation) {
                        continue;
                    }

                    if (trackedAnnotation.activeDevices.Contains(deviceId)) {
                        if (!enterFlag) {
                            _enableDragFromEditorRelease = false;
                        }

                        if (trackedAnnotation.annotation?.onDragFromEditorExit != null) {
                            trackedAnnotation.annotation.onDragFromEditorExit(
                                PointerDragFromEditorExitEvent
                                    .fromDragFromEditorEvent(lastEvent));
                        }

                        trackedAnnotation.activeDevices.Remove(deviceId);
                    }
                }
            }
        }
    }

#endif
}