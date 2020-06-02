#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA
using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;

using Usages = UnityEngine.InputSystem.CommonUsages;

using InputDeviceRole = UnityEngine.XR.InputDeviceRole;

using DeviceRole = UnityEngine.XR.InputDeviceRole;

internal class XRTests : InputTestFixture
{

#if UNITY_2019_3_OR_NEWER
    [Test]
    [Category("Devices")]
    [TestCase(InputDeviceCharacteristics.HeadMounted, "XRHMD", typeof(XRHMD))]
    [TestCase((InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller), "XRController", typeof(XRController))]
    [TestCase((InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left), "XRController", typeof(XRController))]
    [TestCase(InputDeviceCharacteristics.TrackedDevice, null, typeof(UnityEngine.InputSystem.InputDevice))]
    [TestCase(InputDeviceCharacteristics.None, null, typeof(UnityEngine.InputSystem.InputDevice))]
    public void Devices_XRDeviceCharacteristicsDeterminesTypeOfDevice(InputDeviceCharacteristics characteristics, string baseLayoutName, Type expectedType)
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByType(characteristics);
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        Assert.That(createdDevice, Is.TypeOf(expectedType));

        var generatedLayout = InputSystem.LoadLayout(
            $"{XRUtilities.InterfaceCurrent}::{deviceDescription.manufacturer}::{deviceDescription.product}");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.baseLayouts, Is.EquivalentTo(new[] { new InternedString(baseLayoutName) }));
    }
#else
    [Test]
    [Category("Devices")]
    [TestCase(InputDeviceRole.Generic, "XRHMD", typeof(XRHMD))]
    [TestCase(InputDeviceRole.LeftHanded, "XRController", typeof(XRController))]
    [TestCase(InputDeviceRole.RightHanded, "XRController", typeof(XRController))]
    [TestCase(InputDeviceRole.HardwareTracker, null, typeof(UnityEngine.InputSystem.InputDevice))]
    [TestCase(InputDeviceRole.TrackingReference, null, typeof(UnityEngine.InputSystem.InputDevice))]
    [TestCase(InputDeviceRole.GameController, null, typeof(UnityEngine.InputSystem.InputDevice))]
    [TestCase(InputDeviceRole.Unknown, null, typeof(UnityEngine.InputSystem.InputDevice))]
    public void Devices_XRDeviceRoleDeterminesTypeOfDevice(InputDeviceRole role, string baseLayoutName, Type expectedType)
    {
        var deviceDescription = CreateSimpleDeviceDescriptionByType(role);
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        Assert.That(createdDevice, Is.TypeOf(expectedType));

        var generatedLayout = InputSystem.LoadLayout(
            $"{XRUtilities.InterfaceCurrent}::{deviceDescription.manufacturer}::{deviceDescription.product}");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.baseLayouts, Is.EquivalentTo(new[] { new InternedString(baseLayoutName) }));
    }
#endif

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeHandednessOfXRController()
    {
#if UNITY_2019_3_OR_NEWER
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left);
#else
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceRole.LeftHanded);
#endif

        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var controller = InputSystem.devices[0];

        Assert.That(controller.usages, Has.Exactly(1).EqualTo(Usages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(0).EqualTo(Usages.RightHand));
        Assert.That(XRController.rightHand, Is.Null);
        Assert.That(XRController.leftHand, Is.EqualTo(controller));

        InputSystem.SetDeviceUsage(controller, Usages.RightHand);

        Assert.That(controller.usages, Has.Exactly(0).EqualTo(Usages.LeftHand));
        Assert.That(controller.usages, Has.Exactly(1).EqualTo(Usages.RightHand));
        Assert.That(XRController.rightHand, Is.EqualTo(controller));
        Assert.That(XRController.leftHand, Is.Null);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutIsNamespacedAsInterfaceManufacturerDevice()
    {
#if UNITY_2019_3_OR_NEWER
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics.HeadMounted);
#else
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceRole.Generic);
#endif
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var expectedLayoutName =
            $"{XRUtilities.InterfaceCurrent}::{deviceDescription.manufacturer}::{deviceDescription.product}";
        Assert.AreEqual(createdDevice.layout, expectedLayoutName);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutWithoutManufacturer_IsNamespacedAsInterfaceDevice()
    {
#if UNITY_2019_3_OR_NEWER
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics.HeadMounted);
#else
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceRole.Generic);
#endif
        deviceDescription.manufacturer = null;
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var expectedLayoutName = $"{XRUtilities.InterfaceCurrent}::{deviceDescription.product}";
        Assert.AreEqual(expectedLayoutName, createdDevice.layout);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRGeneratedLayoutNames_OnlyContainAllowedCharacters()
    {
        runtime.ReportNewInputDevice(CreateMangledNameDeviceDescription().ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        Assert.AreEqual(createdDevice.layout, "XRInputV1::Manufacturer::XRThisLayoutShouldhave1ValidName");
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRLayoutFeatures_OnlyContainAllowedCharacters()
    {
        runtime.ReportNewInputDevice(CreateMangledNameDeviceDescription().ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        var generatedLayout = InputSystem.LoadLayout(createdDevice.layout);
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(kNumBaseHMDControls + 1));

        var childControl = generatedLayout["SimpleFeature1"];
        Assert.That(childControl.name, Is.EqualTo(new InternedString("SimpleFeature1")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_XRDevicesWithNoOrInvalidCapabilities_DoNotCreateLayouts()
    {
#if UNITY_2019_3_OR_NEWER
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics.HeadMounted);
#else
        var deviceDescription = CreateSimpleDeviceDescriptionByType(InputDeviceRole.Generic);
#endif
        deviceDescription.capabilities = null;
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.LoadLayout("XRInput::Manufacturer::Device");
        Assert.That(generatedLayout, Is.Null);
        Assert.That(InputSystem.devices, Is.Empty);

        deviceDescription.capabilities = "Not a JSON String";
        runtime.ReportNewInputDevice(deviceDescription.ToJson());

        InputSystem.Update();

        generatedLayout = InputSystem.LoadLayout("XRInput::XRManufacturer::Device");
        Assert.That(generatedLayout, Is.Null);
        Assert.That(InputSystem.devices, Is.Empty);
    }

    [Test]
    [Category("State")]
    public void State_AllFeatureTypes_ReadTheSameAsTheirStateValue()
    {
        runtime.ReportNewInputDevice(TestXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var device = InputSystem.devices[0];

        InputSystem.QueueStateEvent(device, new TestXRDeviceState
        {
            button = 0,
            discreteState = 0,
            axis = 0f,
            axis2D = Vector2.zero,
            axis3D = Vector3.zero,
            rotation = Quaternion.identity,
            lastElement = 0,
        });
        InputSystem.Update();

        Assert.That(((ButtonControl)device["Button"]).isPressed, Is.False);
        Assert.That(device["DiscreteState"].ReadValueAsObject(), Is.EqualTo(0));
        Assert.That(device["Axis"].ReadValueAsObject(), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(device["Vector2"].ReadValueAsObject(), Is.EqualTo(Vector2.zero));
        Assert.That(device["Vector3"].ReadValueAsObject(), Is.EqualTo(Vector3.zero));
        Assert.That(device["Rotation"].ReadValueAsObject(), Is.EqualTo(Quaternion.identity));
        Assert.That(device.TryGetChildControl("Custom"), Is.Null);
        Assert.That(((ButtonControl)device["Last"]).isPressed, Is.False);

        InputSystem.QueueStateEvent(device, new TestXRDeviceState
        {
            button = 1,
            discreteState = 17,
            axis = 1.24f,
            axis2D = new Vector2(0.1f, 0.2f),
            axis3D = new Vector3(0.3f, 0.4f, 0.5f),
            rotation = new Quaternion(0.6f, 0.7f, 0.8f, 0.9f),
            lastElement = byte.MaxValue,
        });
        InputSystem.Update();

        Assert.That(((ButtonControl)device["Button"]).isPressed, Is.True);
        Assert.That(device["DiscreteState"].ReadValueAsObject(), Is.EqualTo(17));
        Assert.That(device["Axis"].ReadValueAsObject(), Is.EqualTo(1.24f).Within(0.0001f));
        Assert.That(device["Vector2"].ReadValueAsObject(), Is.EqualTo(new Vector2(0.1f, 0.2f)));
        Assert.That(device["Vector3"].ReadValueAsObject(), Is.EqualTo(new Vector3(0.3f, 0.4f, 0.5f)));
        Assert.That(device["Rotation"].ReadValueAsObject(), Is.EqualTo(new Quaternion(0.6f, 0.7f, 0.8f, 0.9f)));
        Assert.That(device.TryGetChildControl("Custom"), Is.Null);
        Assert.That(((ButtonControl)device["Last"]).isPressed, Is.True);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_AllFeatureTypes_AreRepresentedInTheGeneratedLayout()
    {
        runtime.ReportNewInputDevice(TestXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.LoadLayout("XRInputV1::XRManufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(kNumBaseHMDControls + 7));

        var binaryControl = generatedLayout["Button"];
        Assert.That(binaryControl.name, Is.EqualTo(new InternedString("Button")));
        Assert.That(binaryControl.offset, Is.EqualTo(0));
        Assert.That(binaryControl.layout, Is.EqualTo(new InternedString("Button")));
        Assert.That(binaryControl.usages.Count, Is.EqualTo(1));
        Assert.That(binaryControl.usages[0], Is.EqualTo(new InternedString("ButtonUsage")));

        var discreteControl = generatedLayout["DiscreteState"];
        Assert.That(discreteControl.name, Is.EqualTo(new InternedString("DiscreteState")));
        Assert.That(discreteControl.offset, Is.EqualTo(4));
        Assert.That(discreteControl.layout, Is.EqualTo(new InternedString("Integer")));
        Assert.That(discreteControl.usages.Count, Is.EqualTo(1));
        Assert.That(discreteControl.usages[0], Is.EqualTo(new InternedString("DiscreteStateUsage")));

        var axisControl = generatedLayout["Axis"];
        Assert.That(axisControl.name, Is.EqualTo(new InternedString("Axis")));
        Assert.That(axisControl.offset, Is.EqualTo(8));
        Assert.That(axisControl.layout, Is.EqualTo(new InternedString("Analog")));
        Assert.That(axisControl.usages.Count, Is.EqualTo(1));
        Assert.That(axisControl.usages[0], Is.EqualTo(new InternedString("Axis1DUsage")));

        var vec2Control = generatedLayout["Vector2"];
        Assert.That(vec2Control.name, Is.EqualTo(new InternedString("Vector2")));
        Assert.That(vec2Control.offset, Is.EqualTo(12));
        Assert.That(vec2Control.layout, Is.EqualTo(new InternedString("Vector2")));
        Assert.That(vec2Control.usages.Count, Is.EqualTo(1));
        Assert.That(vec2Control.usages[0], Is.EqualTo(new InternedString("Axis2DUsage")));

        var vec3Control = generatedLayout["Vector3"];
        Assert.That(vec3Control.name, Is.EqualTo(new InternedString("Vector3")));
        Assert.That(vec3Control.offset, Is.EqualTo(20));
        Assert.That(vec3Control.layout, Is.EqualTo(new InternedString("Vector3")));
        Assert.That(vec3Control.usages.Count, Is.EqualTo(1));
        Assert.That(vec3Control.usages[0], Is.EqualTo(new InternedString("Axis3DUsage")));

        var rotationControl = generatedLayout["Rotation"];
        Assert.That(rotationControl.name, Is.EqualTo(new InternedString("Rotation")));
        Assert.That(rotationControl.offset, Is.EqualTo(32));
        Assert.That(rotationControl.layout, Is.EqualTo(new InternedString("Quaternion")));
        Assert.That(rotationControl.usages.Count, Is.EqualTo(1));
        Assert.That(rotationControl.usages[0], Is.EqualTo(new InternedString("RotationUsage")));

        // Custom element is skipped, but occupies 256 bytes

        var lastControl = generatedLayout["Last"];
        Assert.That(lastControl.name, Is.EqualTo(new InternedString("Last")));
        Assert.That(lastControl.offset, Is.EqualTo(304));
        Assert.That(lastControl.layout, Is.EqualTo(new InternedString("Button")));
        Assert.That(lastControl.usages.Count, Is.EqualTo(2));
        Assert.That(lastControl.usages[0], Is.EqualTo(new InternedString("LastElementUsage")));
        Assert.That(lastControl.usages[1], Is.EqualTo(new InternedString("SecondUsage")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ButtonsArePackedByTheByte_WhileLargerStructuresAreFourByteAligned()
    {
        runtime.ReportNewInputDevice(ButtonPackedXRDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var generatedLayout = InputSystem.LoadLayout("XRInputV1::XRManufacturer::XRDevice");
        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls.Count, Is.EqualTo(kNumBaseHMDControls + 8));

        var currentControl = generatedLayout["Button1"];
        Assert.That(currentControl.offset, Is.EqualTo(0));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button2"];
        Assert.That(currentControl.offset, Is.EqualTo(1));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button3"];
        Assert.That(currentControl.offset, Is.EqualTo(2));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button4"];
        Assert.That(currentControl.offset, Is.EqualTo(3));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button5"];
        Assert.That(currentControl.offset, Is.EqualTo(4));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Button6"];
        Assert.That(currentControl.offset, Is.EqualTo(5));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));

        currentControl = generatedLayout["Axis1"];
        Assert.That(currentControl.offset, Is.EqualTo(8));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Analog")));

        currentControl = generatedLayout["Button7"];
        Assert.That(currentControl.offset, Is.EqualTo(12));
        Assert.That(currentControl.layout, Is.EqualTo(new InternedString("Button")));
    }

    [InputControlLayout(updateBeforeRender = true)]
    [Preserve]
    private class TestHMD : UnityEngine.InputSystem.InputDevice
    {
        [InputControl]
        public QuaternionControl quaternion { get; private set; }
        [InputControl]
        public Vector3Control vector3 { get; private set; }
        protected override void FinishSetup()
        {
            base.FinishSetup();
            quaternion = GetChildControl<QuaternionControl>("quaternion");
            vector3 = GetChildControl<Vector3Control>("vector3");
        }
    }

    [Test]
    [Category("Components")]
    public void Components_CanUpdateGameObjectTransformThroughTrackedPoseDriver()
    {
        var position = new Vector3(1.0f, 2.0f, 3.0f);
        var rotation = new Quaternion(0.09853293f, 0.09853293f, 0.09853293f, 0.9853293f);

        var go = new GameObject();
        var tpd = go.AddComponent<TrackedPoseDriver>();
        var device = InputSystem.AddDevice<TestHMD>();

        using (StateEvent.From(device, out var stateEvent))
        {
            var positionAction = new InputAction();
            positionAction.AddBinding("<TestHMD>/vector3");

            var rotationAction = new InputAction();
            rotationAction.AddBinding("<TestHMD>/quaternion");

            tpd.positionAction = positionAction;
            tpd.rotationAction = rotationAction;

            // before render only
            var go1 = tpd.gameObject;
            go1.transform.position = Vector3.zero;
            go1.transform.rotation = new Quaternion(0, 0, 0, 0);
            tpd.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

            device.quaternion.WriteValueIntoEvent(rotation, stateEvent);
            device.vector3.WriteValueIntoEvent(position, stateEvent);

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(position));
            Assert.That(!tpd.gameObject.transform.rotation.Equals(rotation));

            var go2 = tpd.gameObject;
            go2.transform.position = Vector3.zero;
            go2.transform.rotation = new Quaternion(0, 0, 0, 0);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(position));
            Assert.That(tpd.gameObject.transform.rotation.Equals(rotation));

            // update only
            var go3 = tpd.gameObject;
            go3.transform.position = Vector3.zero;
            go3.transform.rotation = new Quaternion(0, 0, 0, 0);
            tpd.updateType = TrackedPoseDriver.UpdateType.Update;
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(position));
            Assert.That(tpd.gameObject.transform.rotation.Equals(rotation));

            var go4 = tpd.gameObject;
            go4.transform.position = Vector3.zero;
            go4.transform.rotation = new Quaternion(0, 0, 0, 0);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(position));
            Assert.That(!tpd.gameObject.transform.rotation.Equals(rotation));

            // check the rot/pos case also Update AND Render.
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            tpd.trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
            var go5 = tpd.gameObject;
            go5.transform.position = Vector3.zero;
            go5.transform.rotation = new Quaternion(0, 0, 0, 0);

            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(tpd.gameObject.transform.position, Is.EqualTo(position));
            Assert.That(!tpd.gameObject.transform.rotation.Equals(rotation));

            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            var go6 = tpd.gameObject;
            go6.transform.position = Vector3.zero;
            go6.transform.rotation = new Quaternion(0, 0, 0, 0);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update(InputUpdateType.BeforeRender);
            Assert.That(tpd.gameObject.transform.position, Is.Not.EqualTo(position));
            Assert.That(tpd.gameObject.transform.rotation.Equals(rotation));
        }
    }

    private const int kNumBaseHMDControls = 10;

#if UNITY_2019_3_OR_NEWER
    InputDeviceRole RoleFromCharacteristics(InputDeviceCharacteristics characteristics)
    {
        if ((characteristics & InputDeviceCharacteristics.Left) != 0)
            return InputDeviceRole.LeftHanded;
        if ((characteristics & InputDeviceCharacteristics.Right) != 0)
            return InputDeviceRole.RightHanded;
        if ((characteristics & InputDeviceCharacteristics.TrackingReference) != 0)
            return InputDeviceRole.TrackingReference;
        if ((characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
            return InputDeviceRole.Generic;
        if ((characteristics & InputDeviceCharacteristics.HeldInHand) != 0)
            return InputDeviceRole.Generic;
        if ((characteristics & InputDeviceCharacteristics.EyeTracking) != 0)
            return InputDeviceRole.Generic;
        if ((characteristics & InputDeviceCharacteristics.Camera) != 0)
            return InputDeviceRole.Generic;
        if ((characteristics & InputDeviceCharacteristics.Controller) != 0)
            return InputDeviceRole.GameController;
        if ((characteristics & InputDeviceCharacteristics.TrackedDevice) != 0)
            return InputDeviceRole.HardwareTracker;

        return InputDeviceRole.LegacyController;
    }
#endif

#if UNITY_2019_3_OR_NEWER
    private static InputDeviceDescription CreateSimpleDeviceDescriptionByType(InputDeviceCharacteristics deviceCharacteristics)
#else
    private static InputDeviceDescription CreateSimpleDeviceDescriptionByType(InputDeviceRole role)
#endif
    {
        return new InputDeviceDescription
        {
            interfaceName = XRUtilities.InterfaceCurrent,
            product = "Device",
            manufacturer = "Manufacturer",
            capabilities = new XRDeviceDescriptor
            {
#if UNITY_2019_3_OR_NEWER
                characteristics = deviceCharacteristics,
#else
                deviceRole = role,
#endif
                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "Filler",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        };
    }

    private static InputDeviceDescription CreateMangledNameDeviceDescription()
    {
        return new InputDeviceDescription
        {
            interfaceName = XRUtilities.InterfaceCurrent,
            product = "XR_This.Layout/Should have 1 Valid::Name",
            manufacturer = "__Manufacturer::",
            capabilities = new XRDeviceDescriptor
            {
#if UNITY_2019_3_OR_NEWER
                characteristics = InputDeviceCharacteristics.HeadMounted,
#else
                deviceRole = InputDeviceRole.Generic,
#endif

                inputFeatures = new List<XRFeatureDescriptor>()
                {
                    new XRFeatureDescriptor()
                    {
                        name = "SimpleFeature[|.:+-?<1",
                        featureType = FeatureType.Binary
                    }
                }
            }.ToJson()
        };
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ButtonPackedXRDeviceState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte button1;
        [FieldOffset(1)] public byte button2;
        [FieldOffset(2)] public byte button3;
        [FieldOffset(3)] public byte button4;
        [FieldOffset(4)] public byte button5;
        [FieldOffset(5)] public byte button6;
        [FieldOffset(8)] public float axis1;
        [FieldOffset(12)] public byte button7;

        public static InputDeviceDescription CreateDeviceDescription()
        {
            return new InputDeviceDescription
            {
                interfaceName = XRUtilities.InterfaceCurrent,
                product = "XRDevice",
                manufacturer = "XRManufacturer",
                capabilities = new XRDeviceDescriptor
                {
#if UNITY_2019_3_OR_NEWER
                    characteristics = InputDeviceCharacteristics.HeadMounted,
#else
                    deviceRole = InputDeviceRole.Generic,
#endif
                    inputFeatures = new List<XRFeatureDescriptor>()
                    {
                        new XRFeatureDescriptor()
                        {
                            name = "Button1",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button2",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button3",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button4",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button5",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button6",
                            featureType = FeatureType.Binary
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Axis1",
                            featureType = FeatureType.Axis1D
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Button7",
                            featureType = FeatureType.Binary
                        },
                    }
                }.ToJson()
            };
        }

        public FourCC format => new FourCC('X', 'R', 'S', '0');
    }

    [StructLayout(LayoutKind.Explicit)]
    unsafe struct TestXRDeviceState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte button;
        [FieldOffset(4)] public uint discreteState;
        [FieldOffset(8)] public float axis;
        [FieldOffset(12)] public Vector2 axis2D;
        [FieldOffset(20)] public Vector3 axis3D;
        [FieldOffset(32)] public Quaternion rotation;
        [FieldOffset(48)] public fixed byte buffer[256];
        [FieldOffset(304)] public byte lastElement;

        public static InputDeviceDescription CreateDeviceDescription()
        {
            return new InputDeviceDescription()
            {
                interfaceName = XRUtilities.InterfaceCurrent,
                product = "XRDevice",
                manufacturer = "XRManufacturer",
                capabilities = new XRDeviceDescriptor
                {
#if UNITY_2019_3_OR_NEWER
                    characteristics = InputDeviceCharacteristics.HeadMounted,
#else
                    deviceRole = InputDeviceRole.Generic,
#endif
                    inputFeatures = new List<XRFeatureDescriptor>()
                    {
                        new XRFeatureDescriptor()
                        {
                            name = "Button",
                            featureType = FeatureType.Binary,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "ButtonUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "DiscreteState",
                            featureType = FeatureType.DiscreteStates,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "DiscreteStateUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Axis",
                            featureType = FeatureType.Axis1D,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "Axis1DUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Vector2",
                            featureType = FeatureType.Axis2D,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "Axis2DUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Vector3",
                            featureType = FeatureType.Axis3D,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "Axis3DUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Rotation",
                            featureType = FeatureType.Rotation,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "RotationUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Custom",
                            featureType = FeatureType.Custom,
                            customSize = 256,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "CustomTypeUsage"
                                }
                            }
                        },
                        new XRFeatureDescriptor()
                        {
                            name = "Last",
                            featureType = FeatureType.Binary,
                            usageHints = new List<UsageHint>()
                            {
                                new UsageHint()
                                {
                                    content = "LastElementUsage"
                                },
                                new UsageHint()
                                {
                                    content = "SecondUsage"
                                }
                            }
                        }
                    }
                }.ToJson()
            };
        }

        public FourCC format
        {
            get { return new FourCC('X', 'R', 'S', '0'); }
        }
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA
