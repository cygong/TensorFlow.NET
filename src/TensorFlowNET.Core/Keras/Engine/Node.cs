﻿/*****************************************************************************
   Copyright 2018 The TensorFlow.NET Authors. All Rights Reserved.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
******************************************************************************/

using System.Collections.Generic;
using System.Linq;
using Tensorflow.Keras.ArgsDefinition;
using static Tensorflow.Binding;

namespace Tensorflow.Keras.Engine
{
    /// <summary>
    /// A `Node` describes the connectivity between two layers.
    /// 
    /// Each time a layer is connected to some new input,
    /// a node is added to `layer._inbound_nodes`.
    /// Each time the output of a layer is used by another layer,
    /// a node is added to `layer._outbound_nodes`.
    /// </summary>
    public class Node
    {
        NodeArgs args;

        public int[] node_indices;
        public int[] tensor_indices;
        public Tensors input_tensors;
        public Tensors Outputs => args.Outputs;
        public TensorShape[] input_shapes;
        public TensorShape[] output_shapes;
        public List<Tensor> KerasInputs = new List<Tensor>();
        public Layer Layer { get; set; }
        public bool IsInput => args.InputTensors == null;
        public int[] FlatInputIds { get; set; }
        public int[] FlatOutputIds { get; set; }

        public Node[] ParentNodes
        {
            get
            {
                var node_deps = new List<Node>();
                foreach(var kt in KerasInputs)
                {
                    var (layer, node_index, _) = kt.KerasHistory;
                    if (layer != null)
                        node_deps.append(layer.InboundNodes[node_index]);
                }
                return node_deps.ToArray();
            }
        } 

        public Node(Layer layer, NodeArgs args)
        {
            this.args = args;
            this.Layer = layer;

            if (args.InputTensors != null)
                KerasInputs.AddRange(args.InputTensors);

            // Wire up Node to Layers.
            layer.InboundNodes.Add(this);
            foreach (var kt in KerasInputs)
            {
                if (kt.KerasHistory == null)
                    continue;
                var (inbound_layer, _, _) = kt.KerasHistory;
                if (inbound_layer != null)
                    inbound_layer.OutboundNodes.Add(this);
            }

            // Set metadata on outputs.
            var node_index = layer.InboundNodes.Count - 1;
            foreach (var (i, tensor) in enumerate(Outputs))
                tensor.KerasHistory = new KerasHistory(layer, node_index, i, tensor);

            // Cached for performance.
            FlatInputIds = KerasInputs.Select(x => x.GetHashCode()).ToArray();
            FlatOutputIds = Outputs.Select(x => x.GetHashCode()).ToArray();
        }
    }
}
