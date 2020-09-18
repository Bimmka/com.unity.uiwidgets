﻿using System.Collections.Generic;
using System.Text;

namespace Unity.UIWidgets.ui {
    public class ParagraphBuilder {
        StringBuilder _text = new StringBuilder();
        ParagraphStyle _paragraphStyle;
        StyledRuns _runs = new StyledRuns();
        List<int> _styleStack = new List<int>();
        int _paragraph_style_index;

        public ParagraphBuilder(ParagraphStyle style) {
            setParagraphStyle(style);
        }

        public Paragraph build() {
            _runs.endRunIfNeeded(_text.Length);
            var paragraph = Paragraph.create();
            paragraph.setText(_text.ToString(), _runs);
            paragraph.setParagraphStyle(_paragraphStyle);
            return paragraph;
        }

        public void pushStyle(painting.TextStyle style, float textScaleFactor) {
            var newStyle = TextStyle.applyStyle(peekStyle(), style, textScaleFactor: textScaleFactor);
            var styleIndex = _runs.addStyle(newStyle);
            _styleStack.Add(styleIndex);
            _runs.startRun(styleIndex, _text.Length);
        }

        internal void pushStyle(TextStyle style) {
            var styleIndex = _runs.addStyle(style);
            _styleStack.Add(styleIndex);
            _runs.startRun(styleIndex, _text.Length);
        }

        public void pop() {
            var lastIndex = _styleStack.Count - 1;
            if (lastIndex < 0) {
                return;
            }

            _styleStack.RemoveAt(lastIndex);
            _runs.startRun(peekStyleIndex(), _text.Length);
        }

        public void addText(string text) {
            _text.Append(text);
        }

        internal TextStyle peekStyle() {
            return _runs.getStyle(peekStyleIndex());
        }


        public int peekStyleIndex() {
            int count = _styleStack.Count;
            if (count > 0) {
                return _styleStack[count - 1];
            }

            return _paragraph_style_index;
        }

        void setParagraphStyle(ParagraphStyle style) {
            _paragraphStyle = style;
            _paragraph_style_index = _runs.addStyle(style.getTextStyle());
            _runs.startRun(_paragraph_style_index, _text.Length);
        }
    }
}