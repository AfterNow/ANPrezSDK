﻿// Copyright 2020-2021 Andreas Atteneder
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
//

namespace GLTFast.Schema {

    [System.Serializable]
    public class AccessorSparse {
        /// <summary>
        /// Number of entries stored in the sparse array.
        /// <minimum>1</minimum>
        /// </summary>
        public int count;

        /// <summary>
        /// Index array of size `count` that points to those accessor attributes that
        /// deviate from their initialization value. Indices must strictly increase.
        /// </summary>
        public AccessorSparseIndices indices;

        /// <summary>
        /// "Array of size `count` times number of components, storing the displaced
        /// accessor attributes pointed by `indices`. Substituted values must have
        /// the same `componentType` and number of components as the base accessor.
        /// </summary>
        public AccessorSparseValues values;

        public void GltfSerialize(JsonWriter writer) {
            writer.AddObject();
            writer.AddProperty("count",count);
            if (indices != null) {
                writer.AddProperty("indices");
                indices.GltfSerialize(writer);
            }
            if (values != null) {
                writer.AddProperty("values");
                values.GltfSerialize(writer);
            }
            writer.Close();
        }

    }
}