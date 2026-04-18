namespace Things.Sandbox;

using System;
using Things.Framework.ECS;

static class EntryPoint
{
	private readonly record struct Component1();
	private readonly record struct Component2();
	private readonly record struct Component3();

	static void Main(string[] args)
	{
		Console.WriteLine("Hello, World!");
		var world = new World();

		Console.WriteLine();
		Console.WriteLine("Hello, entities");

		var entities = new Entity[] {
			world.CreateEntity(),
			world.CreateEntity(),
			world.CreateEntity(),
			world.CreateEntity(),
			world.CreateEntity(),
			world.CreateEntity(),
			world.CreateEntity(),
		};

		world.SetComponent(entities[1 - 1], new Component1());
		world.SetComponent(entities[1 - 1], new Component2());

		world.SetComponent(entities[2 - 1], new Component1());
		world.SetComponent(entities[2 - 1], new Component2());

		world.SetComponent(entities[3 - 1], new Component1());
		world.SetComponent(entities[3 - 1], new Component2());
		world.SetComponent(entities[3 - 1], new Component3());

		world.SetComponent(entities[4 - 1], new Component1());
		world.SetComponent(entities[4 - 1], new Component2());
		world.SetComponent(entities[4 - 1], new Component3());

		world.SetComponent(entities[5 - 1], new Component1());
		world.SetComponent(entities[5 - 1], new Component3());

		world.SetComponent(entities[6 - 1], new Component1());
		world.SetComponent(entities[6 - 1], new Component3());

		world.SetComponent(entities[7 - 1], new Component1());
		world.SetComponent(entities[7 - 1], new Component3());

		LogEntities(world, entities);

		Console.WriteLine();
		Console.WriteLine("Hello, filters");

		var filters = new Filter[] {
			world.FilterBuilder
				.Include<Component1>()
				.Include<Component2>()
				.Exclude<Component3>()
				.Build(),
			world.FilterBuilder
				.Include<Component1>()
				.Exclude<Component2>()
				.Build(),
			world.FilterBuilder
				.Include<Component1>()
				.Build(),
			world.FilterBuilder
				.Include<Component1>()
				.Include<Component3>()
				.Build()
		};

		LogFilters(filters);

		Console.WriteLine();
		RemoveComponent<Component3>(world, entities[6 - 1]);
		RemoveComponent<Component1>(world, entities[3 - 1]);

		LogEntities(world, entities);
		LogFilters(filters);

		Console.ReadKey(intercept: true);
	}

	private static void LogEntities(World world, Entity[] entities)
	{
		Console.WriteLine();
		Console.WriteLine("LogEntities");
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

	private static void LogFilters(Filter[] filters)
	{
		Console.WriteLine();
		Console.WriteLine("LogFilters");
		for (int i = 0; i < filters.Length; i++)
		{
			var filter = filters[i];
			Console.Write($"- Filter {i + 1} [X");
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

	private static void RemoveComponent<T>(World world, Entity entity) where T : unmanaged
	{
		Console.WriteLine($"Ok, remove {typeof(T).Name} from {entity}");
		world.RemoveComponent<T>(entity);
	}
}
