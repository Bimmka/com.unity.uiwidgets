﻿using System;
using System.Collections.Generic;
using System.IO;
using Unity.UIWidgets.async2;
using Unity.UIWidgets.engine2;
using Unity.UIWidgets.ui;
using UnityEngine;
using Canvas = Unity.UIWidgets.ui.Canvas;
using Color = Unity.UIWidgets.ui.Color;
using Path = System.IO.Path;
using Rect = Unity.UIWidgets.ui.Rect;

class TextTest : UIWidgetsPanel
{
    private bool check = true;

    void beginFrame(TimeSpan timeStamp)
    {
        // The timeStamp argument to beginFrame indicates the timing information we
        // should use to clock our animations. It's important to use timeStamp rather
        // than reading the system time because we want all the parts of the system to
        // coordinate the timings of their animations. If each component read the
        // system clock independently, the animations that we processed later would be
        // slightly ahead of the animations we processed earlier.

        // PAINT

        Rect paintBounds = Offset.zero & (Window.instance.physicalSize / Window.instance.devicePixelRatio);
        PictureRecorder recorder = new PictureRecorder();
        Canvas canvas = new Canvas(recorder, paintBounds);
        canvas.translate(paintBounds.width / 2.0f, paintBounds.height / 2.0f);

        // Here we determine the rotation according to the timeStamp given to us by
        // the engine.
        float t = (float) timeStamp.TotalMilliseconds / 1800.0f;
        canvas.rotate(Mathf.PI * (t % 2.0f));

        var paint = new Paint();
        paint.color = Color.fromARGB(100, 100, 100, 0);
        canvas.drawRect(Rect.fromLTRB(0, 0, 100.0f, 100.0f), paint);
        Draw(canvas);

        Picture picture = recorder.endRecording();

        // COMPOSITE

        float devicePixelRatio = Window.instance.devicePixelRatio;
        var deviceTransform = new float[16];
        deviceTransform[0] = devicePixelRatio;
        deviceTransform[5] = devicePixelRatio;
        deviceTransform[10] = 1.0f;
        deviceTransform[15] = 1.0f;
        SceneBuilder sceneBuilder = new SceneBuilder();

        sceneBuilder.pushTransform(deviceTransform);
        sceneBuilder.addPicture(Offset.zero, picture);
        sceneBuilder.pop();
        Window.instance.render(sceneBuilder.build());

        // After rendering the current frame of the animation, we ask the engine to
        // schedule another frame. The engine will call beginFrame again when its time
        // to produce the next frame.
        Window.instance.scheduleFrame();
    }

    void Draw(Canvas canvas)
    {
        if (check)
        {
            foreach (var font in fonts)
            {
                var path = Path.Combine(Application.streamingAssetsPath, font.path);
                var fontFile = File.ReadAllBytes(path);
                ui_.loadFontFromList(fontFile, font.name).then(value =>
                {
                    Debug.Log("finish loading");
                    return FutureOr.nil;
                });
            }
            check = false;
        }

        var style = new ParagraphStyle(
            fontFamily: "Arial",
            height: 4,
            strutStyle: new StrutStyle(fontFamily: "ranchers",
                fontFamilyFallback: new List<string>() {"Roboto-RegularB"}, 
                fontSize: 30),
            ellipsis: "and so on..."
        );


        var pb = new ParagraphBuilder(style);
        pb.addText("just for testxxx\n");
        var ts_roboto_regular = new TextStyle(
            color: new Color(0xF000000F),
            decoration: TextDecoration.lineThrough,
            decorationStyle: TextDecorationStyle.doubleLine,
            fontFamily: "Roboto-Regular",
            fontFamilyFallback: new List<string>() {"ranchers"},
            fontSize: 30,
            height: 1.5f
        );
        pb.pushStyle(ts_roboto_regular);
        pb.addText("just for test\n");
        var ts_roboto_regular_bold = new TextStyle(
            color: new Color(0xF000000F),
            decoration: TextDecoration.lineThrough,
            decorationStyle: TextDecorationStyle.doubleLine,
            fontFamily: "Roboto-RegularB",
            fontSize: 30,
            height: 1.5f
        );
        pb.pushStyle(ts_roboto_regular_bold);
        pb.addText("just for test\n");
        var ts_rachers = new TextStyle(
            color: new Color(0xF000000F),
            decoration: TextDecoration.lineThrough,
            decorationStyle: TextDecorationStyle.doubleLine,
            fontFamily: "ranchers",
            fontSize: 30,
            height: 1.5f
        );
        pb.pushStyle(ts_rachers);
        pb.addText("just for test\n");
        var ts = new TextStyle(
            color: new Color(0xFFFF00F0),
            decoration: TextDecoration.lineThrough,
            decorationStyle: TextDecorationStyle.doubleLine,
            fontFamily: "Arial",
            fontSize: 30,
            height: 1.5f
        );
        pb.pushStyle(ts);
        pb.addText("just for test\n 中文测试 分段测试 分段测试 分段测试 分段测试 分段测试 分段测试 分段测试\n1234");
        var ts2 = new TextStyle(
            decoration: TextDecoration.underline,
            decorationStyle: TextDecorationStyle.dashed,
            fontFamily: "Arial",
            fontSize: 40,
            height: 1.5f,
            background: new Paint()
            {
                color = new Color(0xAAFF7F00),
            },
            foreground: new Paint()
            {
                color = new Color(0xAAFFFF00)
            }
        );
        pb.pushStyle(ts2);
        pb.addText("test push one more style");
        pb.pop();
        pb.addText("test pop style");
        pb.addPlaceholder(10, 10, PlaceholderAlignment.baseline, TextBaseline.alphabetic);
        var p = pb.build();
        p.layout(new ParagraphConstraints(300));
        var wordBoundary = p.getWordBoundary(new TextPosition(10));
        Debug.Log(wordBoundary);
        var positionForOffset = p.getPositionForOffset(new Offset(10, 1));
        Debug.Log(positionForOffset);
        var lineBoundary = p.getLineBoundary(new TextPosition(10));
        Debug.Log(lineBoundary);
        foreach (var textBox in p.getBoxesForPlaceholders())
        {
            Debug.Log($"{textBox.bottom} {textBox.direction} {textBox.left} {textBox.right}");
        }
        foreach (var textBox in p.getBoxesForRange(1, 20))
        {
            Debug.Log($"{textBox.bottom} {textBox.direction} {textBox.left} {textBox.right}");
        }
        
        foreach (var lineMetrics in p.computeLineMetrics())
        {
            Debug.Log($"{lineMetrics.height} {lineMetrics.lineNumber}");
        }

        canvas.drawParagraph(p, new Offset(-100, -100));
    }

    protected override void main()
    {
        Window.instance.onBeginFrame = beginFrame;
        Window.instance.scheduleFrame();
    }
}