using Coocoo3D.Present;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coocoo3D.Core
{
    //场景类有助于用户在两个场景间复制数据，而不是有助于销毁物体。
    public class Scene
    {
        public Scene(Coocoo3DMain appBody)
        {
            AppBody = appBody;
        }
        Coocoo3DMain AppBody;
        public List<MMD3DEntity> Entities = new List<MMD3DEntity>();
        public List<Lighting> Lightings = new List<Lighting>();
        public ObservableCollection<ISceneObject> sceneObjects = new ObservableCollection<ISceneObject>();

        public List<MMD3DEntity> EntityLoadList = new List<MMD3DEntity>();
        public List<Lighting> LightingLoadList = new List<Lighting>();
        public List<MMD3DEntity> EntityRemoveList = new List<MMD3DEntity>();

        public void AddSceneObject(MMD3DEntity entity)
        {
            lock (this)
            {
                EntityLoadList.Add(entity);
            }
            sceneObjects.Add(entity);
        }
        public void AddSceneObject(Lighting lighting)
        {
            lock(this)
            {
                LightingLoadList.Add(lighting);
            }
            sceneObjects.Add(lighting);
        }
        public void RemoveSceneObject(MMD3DEntity entity)
        {
            lock(this)
            {
                Entities.Remove(entity);
                EntityRemoveList.Add(entity);
            }
        }
        public void SortObjects()
        {
            lock (AppBody.deviceResources)
            {
                Entities.Clear();
                Lightings.Clear();
                for(int i=0;i<sceneObjects.Count;i++)
                {
                    if(sceneObjects[i] is MMD3DEntity entity)
                    {
                        Entities.Add(entity);
                    }
                    else if(sceneObjects[i] is Lighting lighting)
                    {
                        Lightings.Add(lighting);
                    }
                }
            }
        }
    }
}
