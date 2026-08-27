# Third Party Notices

UnityPMXEditor does not bundle a third-party PMX parser, MMD model, character asset,
texture, shader, or physics backend.

The PMX reader was independently implemented from the publicly documented binary field
layout. Development consulted Felix Jones' community English PMX 2.1 format description:

- <https://gist.github.com/felixjones/f8a06bd48f9da9a4539f>

That document is a format reference, not redistributed source code. See
`Documentation~/PmxFormat.md` for the implemented validation boundary and fixture
provenance.

The package uses Unity APIs and its tests use Unity Test Framework/NUnit through the host
Unity installation. Those products are not redistributed by this repository and remain
subject to their respective terms.

The file `Samples~/MinimalPmxFixture/MinimalFixture.pmx` is authored by this project from
the PMX field layout, contains only one synthetic triangle/material/bone, and is released
under the repository MIT license. It is not derived from a character or MMD model.

Any PMX model imported by a user remains under its own model, texture, character, and
distribution licenses. The UnityPMXEditor MIT license does not apply to user models merely
because the plugin reads or converts them.
