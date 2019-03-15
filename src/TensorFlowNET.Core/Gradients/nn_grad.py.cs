﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tensorflow.Operations;

namespace Tensorflow.Gradients
{
    public class nn_grad
    {
        /// <summary>
        /// Return the gradients for the 2 inputs of bias_op.
        /// </summary>
        /// <param name="op"></param>
        /// <param name="grad"></param>
        /// <returns></returns>
        public static Tensor[] _BiasAddGrad(Operation op, Tensor[] grads)
        {
            var grad = grads[0];
            string data_format = op.get_attr("data_format")?.ToString();
            var bias_add_grad = gen_nn_ops.bias_add_grad(out_backprop: grad, data_format: data_format);
            return new Tensor[] { grad, bias_add_grad };
        }

        public static Tensor[] _ReluGrad(Operation op, Tensor[] grads)
        {
            return new Tensor[] { gen_nn_ops.relu_grad(grads[0], op.outputs[0]) };
        }

        /// <summary>
        /// Gradient function for SoftmaxCrossEntropyWithLogits.
        /// </summary>
        /// <param name="op"></param>
        /// <param name="grad_loss"></param>
        /// <param name="grad_grad"></param>
        /// <returns></returns>
        public static Tensor[] _SoftmaxCrossEntropyWithLogitsGrad(Operation op, Tensor[] grads)
        {
            var grad_loss = grads[0];
            var grad_grad = grads[1];
            var softmax_grad = op.outputs[1];
            var grad = _BroadcastMul(grad_loss, softmax_grad);

            var logits = op.inputs[0];
            if(grad_grad != null && !IsZero(grad_grad))
            {
                throw new NotImplementedException("_SoftmaxCrossEntropyWithLogitsGrad");
            }

            return new Tensor[] 
            {
                grad,
                _BroadcastMul(grad_loss, -nn_ops.log_softmax(logits))
            };
        }

        private static bool IsZero(Tensor g)
        {
            if (new string[] { "ZerosLike", "Zeros" }.Contains(g.op.type))
                return true;

            throw new NotImplementedException("IsZero");
        }

        private static Tensor _BroadcastMul(Tensor vec, Tensor mat)
        {
            vec = array_ops.expand_dims(vec, -1);
            return vec * mat;
        }

        /// <summary>
        /// Return the gradients for TopK.
        /// </summary>
        /// <param name="op"></param>
        /// <param name="grads"></param>
        /// <returns></returns>
        public static Tensor[] _TopKGrad(Operation op, Tensor[] grads)
        {
            var grad = grads[0];
            var _ = grads[1];

            var in_shape = array_ops.shape(op.inputs[0]);
            var ind_shape = array_ops.shape(op.outputs[1]);

            // int32 is not supported on GPU hence up-casting
            var cast = math_ops.cast(ind_shape, TF_DataType.TF_INT64);
            var size = array_ops.size(ind_shape) - 1;
            var ind_lastdim = array_ops.gather(cast, size);

            // Flatten indices to 2D.
            var stack = array_ops.stack(new object[] { -1L, ind_lastdim });
            var ind_2d = array_ops.reshape(op.outputs[1], stack);

            var in_lastdim = array_ops.gather(math_ops.cast(in_shape, TF_DataType.TF_INT64), 
                array_ops.size(in_shape) - 1);
            var outerdim = array_ops.shape(ind_2d);

            // Compute linear indices(flattened to 1D).
            var cast1 = math_ops.cast(outerdim, TF_DataType.TF_INT64);
            var range2 = math_ops.range(0L, cast1 * in_lastdim, in_lastdim);
            var dim2 = array_ops.expand_dims(range2, -1);
            var cast2 = math_ops.cast(dim2, TF_DataType.TF_INT32);
            var ind = array_ops.reshape(ind_2d + cast2, new int[] { -1 });

            throw new NotImplementedException("nn_grad._TopKGrad");
        }
    }
}