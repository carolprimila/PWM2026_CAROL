import { PageHeader } from '@/components/ui'
import { PollsPanel } from './Polls'

export default function PollsPage() {
  return (
    <>
      <PageHeader title="Votações" subtitle="Todas as votações do clube, vinculadas ou não a uma assembleia." />
      <PollsPanel />
    </>
  )
}
