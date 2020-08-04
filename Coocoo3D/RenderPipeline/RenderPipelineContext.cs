﻿using Coocoo3D.Core;
using Coocoo3D.Present;
using Coocoo3DGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.RenderPipeline
{
    public class RenderPipelineContext
    {
        public RenderTexture2D outputRTV;
        public RenderTexture2D outputDSV;

        public RenderTexture2D DSV0;

        public Texture2D TextureLoading;
        public Texture2D TextureError;

        public MMDMesh ndcQuadMesh;

        public DeviceResources deviceResources;
        public GraphicsContext graphicsContext;

        public Settings settings;
        //public Scene scene;

        public List<MMD3DEntity> entities = new List<MMD3DEntity>();
        public List<Lighting> lightings = new List<Lighting>();
        public List<Camera> cameras=new List<Camera>();

        public void ClearCollections()
        {
            entities.Clear();
            lightings.Clear();
            cameras.Clear();
        }
    }
}
