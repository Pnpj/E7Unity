using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections.Generic;

namespace E7.Entities
{
    public abstract class ReactiveCSBase<ReactiveGroup> : ComponentSystem
    where ReactiveGroup : struct, IReactiveGroup 
    {
        private protected abstract IReactiveInjectGroup<ReactiveGroup> InjectedReactivesInGroup { get; }

        /// <summary>
        /// Use `if(ReactsTo<IReactive>...` given that `IReactive` belongs to the group.
        /// </summary>
        protected abstract void OnReaction();
        protected override void OnUpdate()
        {
            //There is a possibility that we have a mono entity but not any reactive entities in `ReactiveMonoCS`.
            for (int i = 0; i < InjectedReactivesInGroup.Entities.Length; i++)
            {
                iteratingEntity = InjectedReactivesInGroup.Entities[i];
                OnReaction();
                PostUpdateCommands.DestroyEntity(InjectedReactivesInGroup.Entities[i]);
            }
        }

        private protected Entity iteratingEntity;
        protected bool ReactsTo<T>(out T reactiveComponent) where T : struct, IReactive
        {
            if (EntityManager.HasComponent<T>(iteratingEntity))
            {
                reactiveComponent = EntityManager.GetComponentData<T>(iteratingEntity);
                return true;
            }
            reactiveComponent = default;
            return false;
        }
    }

    /// <summary>
    /// Get all of entity made from `MonoECS.Issue` and `EntityCommandBuffer.Issue` with reactive components.
    /// 
    /// Process each reactive entities captured in this frame one by one with 
    /// `OnReaction`, all of them will be destroyed automatically. (Runs only once)
    /// </summary>
    public abstract class ReactiveCS<ReactiveGroup> : ReactiveCSBase<ReactiveGroup>
    where ReactiveGroup : struct, IReactiveGroup
    {
        /// <summary>
        /// Captures reactive entities ready to be destroy after the task.
        /// </summary>
        protected struct ReactiveInjectGroup : IReactiveInjectGroup<ReactiveGroup>
        {
            [ReadOnly] public SharedComponentDataArray<ReactiveGroup> reactiveGroups;
            public EntityArray entities;
            public int Length;

            public SharedComponentDataArray<ReactiveGroup> ReactiveGroups => reactiveGroups;
            public EntityArray Entities => entities;
        }
        [Inject] private protected ReactiveInjectGroup injectedReactivesInGroup;

        private protected override IReactiveInjectGroup<ReactiveGroup> InjectedReactivesInGroup => injectedReactivesInGroup;
    }

    /// <summary>
    /// Get all of one type of your `MonoBehaviour` that you have `GameObjectEntity` attached. 
    /// Then also get all of entity made from `MonoECS.Issue` and `EntityCommandBuffer.Issue` with reactive components.
    /// Your `MonoBehaviour` can then take action on them.
    /// 
    /// Process each reactive entities captured in this frame one by one with
    /// `OnReaction`, all of them will be destroyed automatically. (Runs only once)
    /// </summary>
    public abstract class ReactiveMonoCS<ReactiveGroup, MonoComponent> : ReactiveCS<ReactiveGroup>
    where ReactiveGroup : struct, IReactiveGroup
    where MonoComponent : Component
    {
        /// <summary>
        /// Captures your `MonoBehaviour`s
        /// </summary>
        protected struct MonoGroup
        {
            public ComponentArray<MonoComponent> monoComponents { get; }
            public EntityArray entities { get; }
            public int Length;
        }
        [Inject] private protected MonoGroup monoGroup;

        /// <summary>
        /// Get the first `MonoBehaviour` captured. Useful when you know there's only one in the scene to take all the reactive actions.
        /// </summary>
        protected MonoComponent FirstMono => monoGroup.monoComponents[0];

        /// <summary>
        /// Iterate on all `MonoBehaviour` captured.
        /// </summary>
        protected IEnumerable<MonoComponent> MonoComponents
        {
            get
            {
                for (int i = 0; i < monoGroup.Length; i++)
                {
                    yield return monoGroup.monoComponents[i];
                }
            }
        }
    }

    /// <summary>
    /// When you want to make a reactive system that removes that component at the end, this is a nice start.
    /// You can send the whole InjectGroup into the job with [ReadOnly]
    /// Use `InjectedGroup` to get the data.
    /// </summary>
    public abstract class TagResponseJCS<ReactiveComponent> : JobComponentSystem
    where ReactiveComponent : struct, IComponentData, ITag
    {
        protected struct InjectGroup : ITagResponseInjectGroup<ReactiveComponent>
        {
            public ComponentDataArray<ReactiveComponent> reactiveComponents { get; }
            public EntityArray entities { get; }
            public int Length;
        }
        [Inject] private protected InjectGroup injectedGroup;
        protected InjectGroup InjectedGroup => injectedGroup;
    }

    /// <summary>
    /// When you want to make a reactive system with additional data on that entity.
    /// Take the content out before sending them to the job so that `data` can be written to.
    /// Use `InjectedGroup` to get the data.
    /// </summary>
    public abstract class TagResponseDataJCS<ReactiveComponent, DataComponent> : JobComponentSystem
    where ReactiveComponent : struct, IComponentData, ITag
    where DataComponent : struct, IComponentData
    {
        protected struct InjectGroup : ITagResponseDataInjectGroup<ReactiveComponent, DataComponent>
        {
            public ComponentDataArray<ReactiveComponent> reactiveComponents { get; }
            public EntityArray entities { get; }
            public ComponentDataArray<DataComponent> datas { get; }
            public int Length;
        }
        [Inject] private protected InjectGroup injectedGroup;
        protected InjectGroup InjectedGroup => injectedGroup;
    }
}