/*
Drag.cs is part of the VLAB project.
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
using System.Collections;

namespace IExSys
{
    public class Drag : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private RectTransform parentRectTransform;
        private RectTransform parentparentRectTransform;
        private Vector2 originalLocalPointerPosition;
        private Vector3 originalParentLocalPosition;

        void Awake()
        {
            parentRectTransform = transform.parent as RectTransform;
            parentparentRectTransform = parentRectTransform.parent as RectTransform;
        }

        public void OnPointerDown(PointerEventData ped)
        {
            parentRectTransform.SetAsLastSibling();
            originalParentLocalPosition = parentRectTransform.localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentparentRectTransform, ped.position, ped.pressEventCamera, out originalLocalPointerPosition);
        }

        public void OnDrag(PointerEventData ped)
        {
            Vector2 localPointerPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentparentRectTransform, ped.position, ped.pressEventCamera, out localPointerPosition);
            Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

            parentRectTransform.localPosition = originalParentLocalPosition + offsetToOriginal;
        }
    }
}