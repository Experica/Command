/*
Resize.cs is part of the VLAB project.
Copyright (c) 2016 Li Alex Zhang and Contributors

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace IExSys
{
    public class Resize : MonoBehaviour, IPointerDownHandler, IDragHandler,IEndDragHandler
    {
        public Vector2 minSize;
        public Vector2 maxSize;

        private RectTransform parentRectTransform;
        private Vector2 originalLocalPointerPosition;
        private Vector2 originalSizeDelta;

        void Awake()
        {
            var rootcanvassize = (GameObject.Find("Canvas").transform as RectTransform).rect.size;
            minSize = rootcanvassize * 0.2f;
            maxSize = rootcanvassize * 0.9f;
            parentRectTransform = transform.parent as RectTransform;
        }

        public virtual void OnPointerDown(PointerEventData data)
        {
            parentRectTransform.SetAsLastSibling();
            originalSizeDelta = parentRectTransform.sizeDelta;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, data.position, data.pressEventCamera, out originalLocalPointerPosition);
        }

        public virtual void OnDrag(PointerEventData data)
        {
            Vector2 localPointerPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, data.position, data.pressEventCamera, out localPointerPosition);
            var offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

            parentRectTransform.sizeDelta = new Vector2(
                Mathf.Clamp(originalSizeDelta.x + offsetToOriginal.x, minSize.x, maxSize.x),
                Mathf.Clamp(originalSizeDelta.y - offsetToOriginal.y, minSize.y, maxSize.y)
            );
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
        }
    }
}