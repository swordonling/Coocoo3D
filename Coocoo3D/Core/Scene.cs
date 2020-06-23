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
        public ObservableCollection<MMD3DEntity> Entities = new ObservableCollection<MMD3DEntity>();
        public ObservableCollection<Lighting> Lightings = new ObservableCollection<Lighting>();
        public ObservableCollection<ISceneObject> sceneObjects = new ObservableCollection<ISceneObject>();

        public void AddSceneObject(MMD3DEntity entity)
        {
            lock (AppBody.deviceResources)
            {
                Entities.Add(entity);
                sceneObjects.Add(entity);
            }
        }
        public void AddSceneObject(Lighting lighting)
        {
            lock (AppBody.deviceResources)
            {
                Lightings.Add(lighting);
                sceneObjects.Add(lighting);
            }
        }
    }
}
