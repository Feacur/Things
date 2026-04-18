namespace Things.Sandbox;

using System;
using Things.Framework.ECS;

static class EntryPoint
{
	private readonly record struct Component1();
	private readonly record struct Component2();
	private readonly record struct Component3();

	private readonly record struct Relation1();
	private readonly record struct Relation2();

	static void Main(string[] args)
	{
		Console.WriteLine("Hello, World!");
		var world = new World();

		Console.WriteLine();
		Console.WriteLine("Hello, entities");

		world.CreateEntity();
		world.CreateEntity();
		world.CreateEntity();
		world.CreateEntity();
		world.CreateEntity();
		world.CreateEntity();
		world.CreateEntity();

		world.SetComponent(new Entity(1), new Component1());
		world.SetComponent(new Entity(1), new Component2());

		world.SetComponent(new Entity(2), new Component1());
		world.SetComponent(new Entity(2), new Component2());

		world.SetComponent(new Entity(3), new Component1());
		world.SetComponent(new Entity(3), new Component2());
		world.SetComponent(new Entity(3), new Component3());

		world.SetComponent(new Entity(4), new Component1());
		world.SetComponent(new Entity(4), new Component2());
		world.SetComponent(new Entity(4), new Component3());

		world.SetComponent(new Entity(5), new Component1());
		world.SetComponent(new Entity(5), new Component3());

		world.SetComponent(new Entity(6), new Component1());
		world.SetComponent(new Entity(6), new Component3());

		world.SetComponent(new Entity(7), new Component1());
		world.SetComponent(new Entity(7), new Component3());

		LogEntities(world);

		world.SetRelation(new Entity(1), new Entity(2), new Relation1());
		world.SetRelation(new Entity(1), new Entity(3), new Relation1());
		world.SetRelation(new Entity(1), new Entity(4), new Relation1());
		world.SetRelation(new Entity(1), new Entity(5), new Relation1());

		world.SetRelation(new Entity(3), new Entity(4), new Relation2());
		world.SetRelation(new Entity(3), new Entity(5), new Relation2());
		world.SetRelation(new Entity(3), new Entity(6), new Relation2());
		world.SetRelation(new Entity(3), new Entity(7), new Relation2());

		LogRelations<Relation1>(world);
		LogRelations<Relation2>(world);

		Console.WriteLine();
		Console.WriteLine("Hello, filters");

		world.FilterBuilder
			.Include<Component1>()
			.Include<Component2>()
			.Exclude<Component3>()
			.Build();
		world.FilterBuilder
			.Include<Component1>()
			.Exclude<Component2>()
			.Build();
		world.FilterBuilder
			.Include<Component1>()
			.Build();
		world.FilterBuilder
			.Include<Component1>()
			.Include<Component3>()
			.Build();

		LogFilters(world);

		Console.WriteLine();
		RemoveComponent<Component3>(world, new Entity(6));
		RemoveComponent<Component1>(world, new Entity(3));

		LogEntities(world);
		LogFilters(world);

		Console.WriteLine();
		RemoveRelation<Relation1>(world, new Entity(1), new Entity(3));
		RemoveRelation<Relation2>(world, new Entity(3), new Entity(6));

		LogRelations<Relation1>(world);
		LogRelations<Relation2>(world);

		Console.WriteLine();
		RemoveEntity(world, new Entity(4));

		LogEntities(world);
		LogFilters(world);

		LogRelations<Relation1>(world);
		LogRelations<Relation2>(world);

		Console.WriteLine();
		RemoveEntity(world, new Entity(1));
		RemoveEntity(world, new Entity(7));

		LogEntities(world);
		LogFilters(world);

		LogRelations<Relation1>(world);
		LogRelations<Relation2>(world);

		Console.ReadKey(intercept: true);
	}

	private static void LogEntities(World world)
	{
		Console.WriteLine();
		Console.WriteLine("LogEntities");
		var entities = world.DebugEntities();
		foreach (var entity in entities)
		{
			Console.WriteLine($"- {entity}");
			var component_types = world.DebugComponentTypes(entity);
			Console.Write("  [X");
			foreach (var component_type in component_types)
				Console.Write($" & {component_type.Id}");
			Console.WriteLine("]");
		}
	}

	private static void LogFilters(World world)
	{
		Console.WriteLine();
		Console.WriteLine("LogFilters");
		var filters = world.DebugFilters();
		foreach (var filter in filters)
		{
			Console.Write($"- Filter [X");
			foreach (var component_type in filter.DebugIncludes())
				Console.Write($" & {component_type.Id}");
			foreach (var component_type in filter.DebugExcludes())
				Console.Write($" & ~{component_type.Id}");
			Console.WriteLine("]");
			Console.Write("  ");
			foreach (var entity in filter.AsSpan())
				Console.Write($"#{entity.Id} ");
			Console.WriteLine();
		}
	}

	private static void RemoveComponent<T>(World world, in Entity entity) where T : unmanaged
	{
		Console.WriteLine($"Ok, remove {typeof(T).Name} from {entity}");
		world.RemoveComponent<T>(entity);
	}

	private static void LogRelations<T>(World world) where T : unmanaged
	{
		Console.WriteLine();
		Console.WriteLine($"LogRelations {typeof(T).Name}");
		var relations = world.GetRelations<T>();
		foreach (var pair in relations)
			Console.WriteLine($"- {pair}");
	}

	private static void RemoveRelation<T>(World world, in Entity source, in Entity target) where T : unmanaged
	{
		Console.WriteLine($"Ok, remove {typeof(T).Name} from {source} and {target}");
		world.RemoveRelation<T>(in source, in target);
	}

	private static void RemoveEntity(World world, in Entity entity)
	{
		Console.WriteLine($"Ok, remove {entity}");
		world.DestroyEntity(entity);
	}
}
