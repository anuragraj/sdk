﻿using System;
using System.Collections.Generic;
using System.Linq;
using TopDownProteomics.Chemistry;
using TopDownProteomics.Proteomics;

namespace TopDownProteomics.ProForma.Validation
{
    /// <summary>
    /// Base class for modification lookup.
    /// </summary>
    /// <seealso cref="IProteoformModificationLookup" />
    public abstract class ModificationLookupBase<T> : IProteoformModificationLookup where T : IIdentifiable
    {
        private IProteoformMassDelta[]? _modifications;

        /// <summary>
        /// Gets the modification array.
        /// </summary>
        /// <param name="modifications">The modifications.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">modifications</exception>
        protected void SetupModificationArray(IEnumerable<T> modifications)
        {
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            var modArray = new IProteoformMassDelta[10000]; // More IDs than will ever exist
            int maxId = -1;
            foreach (T modification in modifications)
            {
                IChemicalFormula? chemicalFormula = this.GetChemicalFormula(modification);

                int id = Convert.ToInt32(this.RemovePrefix(modification.Id));

                if (chemicalFormula != null)
                    modArray[id] = new ModificationWrapper(modification, chemicalFormula);

                // Keep all the way up to the max passed in, even if it turns out to be NULL
                maxId = Math.Max(maxId, id);
            }

            Array.Resize(ref modArray, maxId + 1);

            _modifications = modArray;
        }

        /// <summary>
        /// Gets the chemical formula.
        /// </summary>
        /// <param name="modification">The modification.</param>
        /// <returns></returns>
        protected abstract IChemicalFormula? GetChemicalFormula(T modification);

        /// <summary>
        /// Determines whether this instance [can handle descriptor] the specified descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <returns>
        /// <c>true</c> if this instance [can handle descriptor] the specified descriptor; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanHandleDescriptor(IProFormaDescriptor descriptor)
        {
            var nonDefault = descriptor.EvidenceType == this.EvidenceType && 
                (descriptor.Key == ProFormaKey.Name || descriptor.Key == ProFormaKey.Identifier);

            if (!this.IsDefaultModificationType)
                return nonDefault;

            // If this is the default modification type, allow one more condition.
            return nonDefault ||
                (descriptor.Key == ProFormaKey.Name && descriptor.EvidenceType == ProFormaEvidenceType.None);
        }

        /// <summary>The ProForma key.</summary>
        protected abstract ProFormaEvidenceType EvidenceType { get; }

        /// <summary>
        /// Removes the prefix.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected abstract string RemovePrefix(string value);

        /// <summary>
        /// Gets a value indicating whether this instance is default modification type.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is default modification type; otherwise, <c>false</c>.
        /// </value>
        protected virtual bool IsDefaultModificationType => false;

        /// <summary>
        /// Gets the modification.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <returns></returns>
        public virtual IProteoformMassDelta GetModification(IProFormaDescriptor descriptor)
        {
            if (_modifications == null)
                throw new Exception("Modification array in not initialized.");

            if (descriptor.Value == null)
                throw new ProteoformModificationLookupException($"Value is NULL in descriptor {descriptor}.");

            if (descriptor.Key == ProFormaKey.Name)
            {
                string value = descriptor.Value;

                IProteoformMassDelta modification = _modifications
                    .SingleOrDefault(x => x != null && ((ModificationWrapper)x).Modification.Name == value);

                if (modification == null)
                    throw new ProteoformModificationLookupException($"Could not find modification using Name in descriptor {descriptor}.");

                return modification;
            }
            else if (descriptor.Key == ProFormaKey.Identifier)
            {
                string value = this.RemovePrefix(descriptor.Value);

                if (int.TryParse(value, out int id))
                {
                    if (id < 0 || id > _modifications.Length - 1 || _modifications[id] == null)
                        throw new ProteoformModificationLookupException($"Could not find modification using ID in descriptor {descriptor}.");

                    return _modifications[id];
                }

                throw new ProteoformModificationLookupException($"Invalid integer in descriptor {descriptor}.");
            }

            throw new ProteoformModificationLookupException($"Couldn't handle value for descriptor {descriptor}.");
        }

        private class ModificationWrapper : IProteoformOntologyDelta
        {
            private IChemicalFormula _chemicalFormula;

            public ModificationWrapper(T modification, IChemicalFormula chemicalFormula)
            {
                this.Modification = modification;
                _chemicalFormula = chemicalFormula;
            }

            public T Modification { get; }

            public string Id => this.Modification.Id;

            public string Name => this.Modification.Name;

            public IChemicalFormula GetChemicalFormula() => _chemicalFormula;

            public ProFormaDescriptor GetProFormaDescriptor()
            {
                throw new NotImplementedException();
            }

            public double GetMass(MassType massType) => _chemicalFormula.GetMass(massType);
        }
    }
}