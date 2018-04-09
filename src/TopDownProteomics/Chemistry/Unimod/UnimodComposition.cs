﻿using System;
using System.Collections.Generic;

namespace TopDownProteomics.Chemistry.Unimod
{
    /// <summary>
    /// The chemical composition of the modification as a delta between the modified and unmodified residue or terminus.
    /// For example, if the modification removes an H and adds a CH3 group, the Composition would be shown as H(2) C. The formula is displayed and entered as 'atoms', optionally followed by a number in parentheses. The number may be negative and, if there is no number, 1 is assumed. Hence, H(2) C is the same as H(2) C(1).
    /// </summary>
    public class UnimodComposition : IHasChemicalFormula
    {
        List<UnimodCompositionAtomCardinality> _atomCardinalities;

        private UnimodComposition()
        {
            _atomCardinalities = new List<UnimodCompositionAtomCardinality>();
        }

        /// <summary>
        /// Creates a Unimod composition from a formula string.
        /// </summary>
        /// <param name="formula">The formula.</param>
        /// <param name="atomProvider">The atom provider.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">formula</exception>
        public static UnimodComposition CreateFromFormula(string formula, IUnimodCompositionAtomProvider atomProvider)
        {
            if (string.IsNullOrEmpty(formula))
                throw new ArgumentNullException(nameof(formula));

            if (atomProvider == null)
                throw new ArgumentNullException(nameof(atomProvider));

            UnimodComposition unimodComposition = new UnimodComposition();

            var atoms = formula.Split(' ');

            for (int i = 0; i < atoms.Length; i++)
            {
                var symbol = atoms[i];
                int count = 1;

                if (symbol.Contains("("))
                {
                    int startIndex = atoms[0].IndexOf("(");
                    symbol = atoms[i].Substring(0, startIndex);
                    count = Convert.ToInt32(atoms[i].Substring(startIndex + 1, atoms[i].Length - startIndex - 2));
                }

                unimodComposition.AddAtom(atomProvider.GetUnimodCompositionAtom(symbol), count);
            }

            return unimodComposition;
        }

        /// <summary>
        /// Gets the chemical formula.
        /// </summary>
        /// <returns></returns>
        public IChemicalFormula GetChemicalFormula()
        {
            IChemicalFormula formula = null;

            foreach (var atom in _atomCardinalities)
            {
                var atomFormula = atom.Atom.GetChemicalFormula().Multiply(atom.Count);

                if (formula == null)
                    formula = atomFormula;
                else
                    formula = formula.Add(atomFormula);
            }

            return formula;
        }

        /// <summary>
        /// Gets the atom cardinalities.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<UnimodCompositionAtomCardinality> GetAtomCardinalities() => _atomCardinalities;

        private void AddAtom(UnimodCompositionAtom atom, int count)
        {
            _atomCardinalities.Add(new UnimodCompositionAtomCardinality(atom, count));
        }
    }
}