"use client";

import { useParams } from "next/navigation";
import { ReportsList } from "@/components/reports-panel";

export default function ClientReportsPage() {
  const params = useParams<{ clientId: string }>();
  return <ReportsList clientId={params.clientId} />;
}
