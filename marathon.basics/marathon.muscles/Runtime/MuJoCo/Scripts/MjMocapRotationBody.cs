// Copyright 2019 DeepMind Technologies Limited
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Xml;
using UnityEngine;
using Mujoco;
using System.Linq;

namespace Mujoco
{
    public class MjMocapRotationBody : MjBaseBody
    {

        int startOffset;

        protected override void OnParseMjcf(XmlElement mjcf)
        {
            throw new Exception("parsing mocap bodies isn't supported.");
        }

        protected override unsafe void OnBindToRuntime(MujocoLib.mjModel_* model, MujocoLib.mjData_* data)
        {
            var bodyId = MujocoLib.mj_name2id(model, (int)ObjectType, MujocoName);
            MujocoId = model->body_mocapid[bodyId];
            startOffset = MjScene.Instance.Model->nmocap * 3 + 4 * MujocoId;
        }

        protected override XmlElement OnGenerateMjcf(XmlDocument doc)
        {
            var mjcf = doc.CreateElement("body");
            MjEngineTool.PositionRotationToMjcf(mjcf, this);
            mjcf.SetAttribute("mocap", "true");
            return mjcf;
        }

        public override unsafe void OnSyncState(MujocoLib.mjData_* data)
        {
            var mjQuat = MjEngineTool.MjQuaternion(transform.rotation);

            data->mocap_quat[startOffset] = mjQuat.w;
            data->mocap_quat[startOffset + 1] = mjQuat.x;
            data->mocap_quat[startOffset + 2] = mjQuat.y;
            data->mocap_quat[startOffset + 3] = mjQuat.z;

        }
    }
}
