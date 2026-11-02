import { describe, expect, it } from 'vitest'
import { sampleProfile } from '../../test/fixtures'
import { emptyExperience, emptyProfile, profileErrorPath, splitList, toFormValues, toProfileRequest } from './profileForm'

describe('splitList', () => {
  it('splits by comma or newline, trims and removes case-insensitive duplicates', () => {
    expect(splitList(' C#, sql\nSQL ,, Docker ')).toEqual(['C#', 'sql', 'Docker'])
  })

  it('can split by newline only, keeping commas inside items', () => {
    expect(splitList('Reduje costos, tiempos y errores\nLideré 3 personas', /\n/)).toEqual([
      'Reduje costos, tiempos y errores',
      'Lideré 3 personas',
    ])
  })
})

describe('form <-> API mapping', () => {
  it('round-trips an existing profile without losing data', () => {
    const { id: _id, updatedAt: _updatedAt, ...request } = sampleProfile

    expect(toProfileRequest(toFormValues(sampleProfile))).toEqual(request)
  })

  it('converts months to dates, blanks to null and drops the end date of a current job', () => {
    const request = toProfileRequest({
      ...emptyProfile,
      fullName: '  Laura  ',
      headline: '   ',
      experiences: [{ ...emptyExperience, company: 'Empresa', role: 'Cargo', startMonth: '2023-04', endMonth: '2024-01', isCurrent: true }],
    })

    expect(request.fullName).toBe('Laura')
    expect(request.headline).toBeNull()
    expect(request.experiences[0]).toMatchObject({ startDate: '2023-04-01', endDate: null, isCurrent: true, location: null })
  })
})

describe('profileErrorPath', () => {
  it.each([
    ['fullName', 'fullName'],
    ['links.linkedInUrl', 'links.linkedInUrl'],
    ['experiences[0].endDate', 'experiences.0.endMonth'],
    ['experiences[2].technologies[1]', 'experiences.2.technologiesText'],
    ['experiences[0].achievements', 'experiences.0.achievementsText'],
    ['education[1].startDate', 'education.1.startMonth'],
    ['certifications[0].issuedOn', 'certifications.0.issuedMonth'],
    ['technicalSkills', 'technicalSkillsText'],
    ['softSkills[3]', 'softSkillsText'],
  ])('maps %s to %s', (apiPath, formPath) => {
    expect(profileErrorPath(apiPath)).toBe(formPath)
  })
})
